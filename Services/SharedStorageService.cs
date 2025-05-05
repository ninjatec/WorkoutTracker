using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text;
using WorkoutTrackerWeb.Services.Redis;
using Microsoft.Extensions.Options;
using System.Linq;

namespace WorkoutTrackerWeb.Services
{
    public interface ISharedStorageService
    {
        /// <summary>
        /// Stores a file in shared storage and returns a unique ID to retrieve it
        /// </summary>
        Task<string> StoreFileAsync(Stream fileStream, string fileExtension = "", TimeSpan? expiry = null);
        
        /// <summary>
        /// Stores a file from a path in shared storage and returns a unique ID to retrieve it
        /// </summary>
        Task<string> StoreFileFromPathAsync(string filePath, TimeSpan? expiry = null);
        
        /// <summary>
        /// Retrieves a file from shared storage as a stream
        /// </summary>
        Task<(Stream fileStream, string extension)> RetrieveFileAsync(string fileId);
        
        /// <summary>
        /// Retrieves a file from shared storage and saves it to a temporary path
        /// </summary>
        Task<string> RetrieveFileToPathAsync(string fileId);
        
        /// <summary>
        /// Deletes a file from shared storage
        /// </summary>
        Task DeleteFileAsync(string fileId);
        
        /// <summary>
        /// Checks if a file exists in shared storage
        /// </summary>
        Task<bool> FileExistsAsync(string fileId);
    }

    public class RedisSharedStorageService : ISharedStorageService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<RedisSharedStorageService> _logger;
        private readonly IRedisKeyService _keyService;
        private const int CHUNK_SIZE = 1024 * 1024; // 1MB chunks
        private const int MAX_RETRIES = 3;
        private const int RETRY_DELAY_MS = 500;

        public RedisSharedStorageService(
            IConnectionMultiplexer redis,
            ILogger<RedisSharedStorageService> logger,
            IRedisKeyService keyService)
        {
            _redis = redis;
            _logger = logger;
            _keyService = keyService;
        }

        private IDatabase GetWritableDatabase()
        {
            if (_redis == null)
            {
                _logger.LogWarning("Redis connection is null, returning empty database implementation. Using local filesystem storage.");
                return null;
            }

            int retryCount = 0;
            int maxRetries = 3;
            int baseDelayMs = 200;

            while (retryCount < maxRetries)
            {
                try
                {
                    if (!_redis.IsConnected)
                    {
                        _logger.LogWarning("Redis is not connected. Attempt {RetryCount}/{MaxRetries} to reconnect...", 
                            retryCount + 1, maxRetries);
                        int delayMs = baseDelayMs * (int)Math.Pow(2, retryCount);
                        Task.Delay(delayMs).Wait();
                        retryCount++;
                        continue;
                    }

                    var db = _redis.GetDatabase();
                    System.Net.EndPoint masterEndpoint = null;
                    foreach (var endpoint in _redis.GetEndPoints())
                    {
                        var server = _redis.GetServer(endpoint);
                        if (!server.IsReplica)
                        {
                            _logger.LogDebug("Found master Redis node at {Endpoint}", endpoint);
                            masterEndpoint = endpoint;
                            break;
                        }
                    }
                    
                    if (masterEndpoint == null)
                    {
                        _logger.LogWarning("No master Redis node found in the cluster! Write operations will fail.");
                        throw new InvalidOperationException("No master Redis node available for write operations");
                    }

                    string testKey = _keyService.CreateKey("test", Guid.NewGuid().ToString());
                    var setTask = db.StringSetAsync(testKey, "test", TimeSpan.FromSeconds(5));
                    
                    if (!setTask.Wait(TimeSpan.FromSeconds(5)))
                    {
                        throw new TimeoutException("Redis write test timed out after 5 seconds");
                    }
                    
                    if (!setTask.Result)
                    {
                        throw new InvalidOperationException("Redis write test failed - SET operation returned false");
                    }
                    
                    return db;
                }
                catch (RedisConnectionException connEx)
                {
                    _logger.LogWarning(connEx, "Redis connection error. Attempt {RetryCount}/{MaxRetries}. Will retry.", 
                        retryCount + 1, maxRetries);
                    int delayMs = baseDelayMs * (int)Math.Pow(2, retryCount);
                    Task.Delay(delayMs).Wait();
                    retryCount++;
                }
                catch (RedisCommandException ex) when (ex.Message.Contains("READONLY") || ex.Message.Contains("replica"))
                {
                    _logger.LogWarning("Connected to a read-only Redis replica. Attempting to find writable master. Error: {Error}", ex.Message);
                    
                    try
                    {
                        var connection = _redis as ConnectionMultiplexer;
                        if (connection != null)
                        {
                            connection.Configure();
                        }
                        
                        int delayMs = baseDelayMs * (int)Math.Pow(2, retryCount);
                        Task.Delay(delayMs).Wait();
                        retryCount++;
                    }
                    catch (Exception endpointEx)
                    {
                        _logger.LogError(endpointEx, "Error while trying to reconfigure Redis connection");
                        throw new InvalidOperationException("Cannot determine Redis master endpoint", endpointEx);
                    }
                }
                catch (Exception ex)
                {
                    if (retryCount < maxRetries - 1)
                    {
                        _logger.LogWarning(ex, "Error checking Redis write capability. Attempt {RetryCount}/{MaxRetries}. Will retry.", 
                            retryCount + 1, maxRetries);
                        int delayMs = baseDelayMs * (int)Math.Pow(2, retryCount);
                        Task.Delay(delayMs).Wait();
                        retryCount++;
                    }
                    else
                    {
                        _logger.LogError(ex, "Error checking Redis write capability after {RetryCount} attempts", maxRetries);
                        _logger.LogWarning("Returning Redis database connection without write verification. Operations may fail.");
                        return _redis.GetDatabase();
                    }
                }
            }
            
            _logger.LogError("Failed to get a writable Redis connection after {MaxRetries} attempts", maxRetries);
            throw new InvalidOperationException($"Failed to get a writable Redis connection after {maxRetries} attempts");
        }

        public async Task<string> StoreFileAsync(Stream fileStream, string fileExtension = "", TimeSpan? expiry = null)
        {
            if (fileStream == null)
            {
                throw new ArgumentNullException(nameof(fileStream));
            }

            string fileId = Guid.NewGuid().ToString();
            string fileKey = _keyService.CreateFileKey(fileId);
            string metaKey = _keyService.CreateFileKey(fileId, "meta");
            
            if (!expiry.HasValue)
            {
                expiry = _keyService.GetExpirationForKeyType(RedisKeyType.File);
            }
            
            var db = GetWritableDatabase();
            
            if (db == null && _redis != null)
            {
                _logger.LogError("Failed to get a writable Redis connection for storing file");
                throw new InvalidOperationException("Failed to get a writable Redis connection for storing file");
            }
            
            if (db == null && _redis == null)
            {
                _logger.LogWarning("Redis is not configured, falling back to local filesystem storage");
                string tempDir = Path.GetTempPath();
                string tempFileName = $"{fileId}{fileExtension}";
                string tempFilePath = Path.Combine(tempDir, tempFileName);
                
                try
                {
                    using (var outputStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
                    {
                        await fileStream.CopyToAsync(outputStream);
                    }
                    
                    _logger.LogInformation("File stored successfully in local filesystem. ID: {FileId}, Path: {FilePath}", 
                        fileId, tempFilePath);
                    return fileId;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error storing file in local filesystem: {Message}", ex.Message);
                    throw;
                }
            }

            int retryCount = 0;
            int maxRetries = 3;
            int retryDelayMs = 500;
            
            while (retryCount < maxRetries)
            {
                try
                {
                    _logger.LogInformation("Storing file in shared storage with ID: {FileId}", fileId);
                    
                    await db.HashSetAsync(metaKey, new HashEntry[]
                    {
                        new HashEntry("extension", fileExtension),
                        new HashEntry("created", DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                    });

                    if (expiry.HasValue)
                    {
                        await db.KeyExpireAsync(metaKey, expiry.Value);
                    }

                    byte[] buffer = new byte[CHUNK_SIZE];
                    int bytesRead;
                    long position = 0;

                    while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        if (bytesRead < buffer.Length)
                        {
                            Array.Resize(ref buffer, bytesRead);
                        }

                        bool chunkStored = false;
                        int chunkRetryCount = 0;
                        
                        while (!chunkStored && chunkRetryCount < 3)
                        {
                            try 
                            {
                                string chunkKey = _keyService.CreateFileKey(fileId, position.ToString());
                                await db.StringSetAsync(chunkKey, buffer);
                                
                                if (expiry.HasValue)
                                {
                                    await db.KeyExpireAsync(chunkKey, expiry.Value);
                                }
                                
                                chunkStored = true;
                            }
                            catch (Exception ex)
                            {
                                chunkRetryCount++;
                                if (chunkRetryCount >= 3)
                                {
                                    _logger.LogError(ex, "Failed to store chunk at position {Position} after 3 retries", position);
                                    throw;
                                }
                                
                                _logger.LogWarning(ex, "Error storing chunk at position {Position}. Retry {RetryCount}/3", 
                                    position, chunkRetryCount);
                                await Task.Delay(retryDelayMs);
                            }
                        }

                        position += bytesRead;
                    }

                    await db.HashSetAsync(metaKey, "size", position);

                    _logger.LogInformation("File stored successfully in shared storage. ID: {FileId}, Size: {Size} bytes", fileId, position);
                    return fileId;
                }
                catch (RedisCommandException ex) when (ex.Message.Contains("READONLY"))
                {
                    retryCount++;
                    if (retryCount >= maxRetries)
                    {
                        _logger.LogError(ex, "Cannot store file - connected to a read-only Redis replica. File ID: {fileId}", fileId);
                        
                        try { await CleanupFileAsync(fileId); } catch { }
                        
                        throw new InvalidOperationException($"Cannot store file - connected to a read-only Redis replica. File ID: {fileId}", ex);
                    }
                    
                    _logger.LogWarning(ex, "Connected to a read-only Redis replica. Attempt {RetryCount}/{MaxRetries}", 
                        retryCount, maxRetries);
                        
                    db = GetWritableDatabase();
                    if (db == null)
                    {
                        _logger.LogError("Failed to get a writable Redis connection after retry");
                        throw new InvalidOperationException("Failed to get a writable Redis connection after retry");
                    }
                    
                    await Task.Delay(retryDelayMs * retryCount);
                }
                catch (RedisConnectionException connEx)
                {
                    retryCount++;
                    if (retryCount >= maxRetries)
                    {
                        _logger.LogError(connEx, "Cannot store file - Redis connection issue. File ID: {FileId}", fileId);
                        
                        try { await CleanupFileAsync(fileId); } catch { }
                        
                        throw;
                    }
                    
                    _logger.LogWarning(connEx, "Redis connection issue. Attempt {RetryCount}/{MaxRetries}", 
                        retryCount, maxRetries);
                    
                    db = GetWritableDatabase();
                    
                    await Task.Delay(retryDelayMs * retryCount);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error storing file in shared storage: {Message}", ex.Message);
                    
                    try { await CleanupFileAsync(fileId); } catch { }
                    
                    throw;
                }
            }
            
            _logger.LogError("Failed to store file after {MaxRetries} retries", maxRetries);
            throw new InvalidOperationException($"Failed to store file after {maxRetries} retries");
        }

        public async Task<string> StoreFileFromPathAsync(string filePath, TimeSpan? expiry = null)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            string fileExtension = Path.GetExtension(filePath);
            
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                return await StoreFileAsync(fileStream, fileExtension, expiry);
            }
        }

        public async Task<(Stream fileStream, string extension)> RetrieveFileAsync(string fileId)
        {
            if (string.IsNullOrEmpty(fileId))
            {
                throw new ArgumentNullException(nameof(fileId));
            }

            if (_redis == null)
            {
                _logger.LogWarning("Redis is not configured, using local filesystem storage for file retrieval");
                string tempDir = Path.GetTempPath();
                
                try
                {
                    string[] possibleFiles = Directory.GetFiles(tempDir, $"{fileId}*");
                    if (possibleFiles.Length > 0)
                    {
                        string filePath = possibleFiles[0];
                        string extension = Path.GetExtension(filePath);
                        var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                        
                        _logger.LogInformation("File retrieved successfully from local filesystem. ID: {FileId}, Path: {FilePath}", 
                            fileId, filePath);
                            
                        return (fileStream, extension);
                    }
                    
                    _logger.LogWarning("File not found in local filesystem: {FileId}", fileId);
                    throw new FileNotFoundException($"File not found in local filesystem: {fileId}");
                }
                catch (Exception ex) when (!(ex is FileNotFoundException))
                {
                    _logger.LogError(ex, "Error retrieving file from local filesystem: {FileId}", fileId);
                    throw;
                }
            }

            string fileKey = _keyService.CreateFileKey(fileId);
            string metaKey = _keyService.CreateFileKey(fileId, "meta");
            
            if (_redis == null)
            {
                throw new InvalidOperationException("Redis connection is not available");
            }
            
            var db = _redis.GetDatabase();
            if (db == null) 
            {
                throw new InvalidOperationException("Failed to get Redis database for file retrieval");
            }

            if (!await db.KeyExistsAsync(metaKey))
            {
                _logger.LogWarning("File not found in shared storage: {FileId}", fileId);
                throw new FileNotFoundException($"File not found in shared storage: {fileId}");
            }

            try
            {
                var metaHash = await db.HashGetAllAsync(metaKey);
                var meta = metaHash.ToDictionary(h => (string)h.Name, h => (string)h.Value);
                
                if (!meta.TryGetValue("size", out string sizeStr) || !long.TryParse(sizeStr, out long size))
                {
                    throw new InvalidDataException($"Invalid file metadata for {fileId}");
                }
                
                string extension = meta.TryGetValue("extension", out string ext) ? ext : "";

                MemoryStream memoryStream = new MemoryStream();
                
                long position = 0;
                while (position < size)
                {
                    string chunkKey = _keyService.CreateFileKey(fileId, position.ToString());
                    var chunk = await db.StringGetAsync(chunkKey);
                    
                    if (chunk.IsNull)
                    {
                        throw new InvalidDataException($"Missing chunk at position {position} for file {fileId}");
                    }
                    
                    byte[] bytes = chunk;
                    await memoryStream.WriteAsync(bytes, 0, bytes.Length);
                    position += bytes.Length;
                }
                
                memoryStream.Position = 0;
                
                _logger.LogInformation("File retrieved successfully from shared storage. ID: {FileId}, Size: {Size} bytes", fileId, size);
                return (memoryStream, extension);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving file from shared storage: {FileId}, {Message}", fileId, ex.Message);
                throw;
            }
        }

        public async Task<string> RetrieveFileToPathAsync(string fileId)
        {
            if (string.IsNullOrEmpty(fileId))
            {
                throw new ArgumentNullException(nameof(fileId));
            }

            try
            {
                var (fileStream, extension) = await RetrieveFileAsync(fileId);
                
                string tempDir = Path.GetTempPath();
                string tempFileName = $"{Path.GetRandomFileName()}{extension}";
                string tempFilePath = Path.Combine(tempDir, tempFileName);
                
                using (var fileStream2 = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
                {
                    await fileStream.CopyToAsync(fileStream2);
                }
                
                fileStream.Dispose();
                
                _logger.LogInformation("File retrieved from shared storage and saved to temp path: {TempFilePath}", tempFilePath);
                return tempFilePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving file from shared storage to path: {FileId}, {Message}", fileId, ex.Message);
                throw;
            }
        }

        public async Task DeleteFileAsync(string fileId)
        {
            if (string.IsNullOrEmpty(fileId))
            {
                throw new ArgumentNullException(nameof(fileId));
            }

            if (_redis == null)
            {
                _logger.LogWarning("Redis is not configured, attempting to delete from local filesystem: {FileId}", fileId);
                string tempDir = Path.GetTempPath();
                try
                {
                    string[] possibleFiles = Directory.GetFiles(tempDir, $"{fileId}*");
                    bool deleted = false;
                    
                    foreach (string filePath in possibleFiles)
                    {
                        try
                        {
                            File.Delete(filePath);
                            deleted = true;
                            _logger.LogInformation("Deleted file from local filesystem: {FilePath}", filePath);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete file from local filesystem: {FilePath}", filePath);
                        }
                    }
                    
                    if (!deleted)
                    {
                        _logger.LogWarning("No matching files found in local filesystem for deletion: {FileId}", fileId);
                    }
                    
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting file from local filesystem: {FileId}", fileId);
                    throw;
                }
            }

            try 
            {
                await CleanupFileAsync(fileId);
                _logger.LogInformation("File deleted from shared storage: {FileId}", fileId);
            }
            catch (RedisCommandException ex) when (ex.Message.Contains("READONLY"))
            {
                _logger.LogError(ex, "Cannot delete file - connected to a read-only Redis replica. File ID: {fileId}", fileId);
                throw new InvalidOperationException($"Cannot delete file - connected to a read-only Redis replica. File ID: {fileId}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file from shared storage: {FileId}, {Message}", fileId, ex.Message);
                throw;
            }
        }

        public async Task<bool> FileExistsAsync(string fileId)
        {
            if (string.IsNullOrEmpty(fileId))
            {
                return false;
            }

            if (_redis == null)
            {
                _logger.LogWarning("Redis is not configured, checking local filesystem for file: {FileId}", fileId);
                string tempDir = Path.GetTempPath();
                try
                {
                    string[] possibleFiles = Directory.GetFiles(tempDir, $"{fileId}*");
                    bool exists = possibleFiles.Length > 0;
                    
                    _logger.LogInformation("File existence check in local filesystem: {FileId}, exists: {Exists}", fileId, exists);
                    return exists;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking file existence in local filesystem: {FileId}", fileId);
                    return false;
                }
            }

            try
            {
                string metaKey = _keyService.CreateFileKey(fileId, "meta");
                
                var db = _redis.GetDatabase();
                
                if (db == null)
                {
                    throw new InvalidOperationException("Failed to get Redis database for file existence check");
                }
                
                return await db.KeyExistsAsync(metaKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if file exists in shared storage: {FileId}", fileId);
                return false;
            }
        }

        private async Task CleanupFileAsync(string fileId)
        {
            if (_redis == null)
            {
                _logger.LogDebug("CleanupFileAsync called in local filesystem mode, delegating to DeleteFileAsync");
                return;
            }

            string fileKey = _keyService.CreateFileKey(fileId);
            string metaKey = _keyService.CreateFileKey(fileId, "meta");
            
            var db = GetWritableDatabase();
            
            if (db == null && _redis != null)
            {
                _logger.LogWarning("Could not get a writable Redis connection for cleanup. File ID: {fileId}", fileId);
                return;
            }
            
            try {
                HashEntry[] metaEntries = await db.HashGetAllAsync(metaKey);
                var meta = metaEntries.ToDictionary(h => (string)h.Name, h => (string)h.Value);
                
                if (meta.TryGetValue("size", out string sizeStr) && long.TryParse(sizeStr, out long size))
                {
                    long position = 0;
                    while (position < size)
                    {
                        string chunkKey = _keyService.CreateFileKey(fileId, position.ToString());
                        RedisValue chunk = RedisValue.Null;
                        
                        try
                        {
                            var getTask = db.StringGetAsync(chunkKey);
                            if (await Task.WhenAny(getTask, Task.Delay(5000)) == getTask)
                            {
                                chunk = await getTask;
                            }
                            else
                            {
                                _logger.LogWarning("Timeout getting chunk at position {Position} for file {fileId}", position, fileId);
                                position += CHUNK_SIZE;
                                continue;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error getting chunk at position {Position} for file {fileId}", position, fileId);
                            position += CHUNK_SIZE;
                            continue;
                        }
                        
                        if (!chunk.IsNull)
                        {
                            try 
                            {
                                await db.KeyDeleteAsync(chunkKey);
                                byte[] bytes = chunk;
                                position += bytes.Length;
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Error deleting chunk at position {Position} for file {fileId}", position, fileId);
                                position += CHUNK_SIZE;
                            }
                        }
                        else
                        {
                            position += CHUNK_SIZE;
                        }
                    }
                }
                else
                {
                    try
                    {
                        if (_redis.GetEndPoints().Length > 0)
                        {
                            var server = _redis.GetServer(_redis.GetEndPoints()[0]);
                            string pattern = _keyService.GetKeyPatternForEntityType("file");
                            var keysTask = Task.Run(() => server.Keys(pattern: pattern).ToArray());
                            RedisKey[] keys;
                            
                            if (await Task.WhenAny(keysTask, Task.Delay(10000)) == keysTask)
                            {
                                keys = await keysTask;
                                
                                foreach (var key in keys.Where(k => k.ToString().Contains(fileId)))
                                {
                                    try 
                                    {
                                        await db.KeyDeleteAsync(key);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning(ex, "Error deleting key {Key} for file {fileId}", key, fileId);
                                    }
                                }
                            }
                            else
                            {
                                _logger.LogWarning("Timeout searching for chunk keys for file {fileId}", fileId);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error searching for chunk keys for file {fileId}", fileId);
                    }
                }
                
                try
                {
                    await db.KeyDeleteAsync(metaKey);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error deleting metadata for file {fileId}", fileId);
                }
            }
            catch (RedisCommandException ex) when (ex.Message.Contains("READONLY"))
            {
                _logger.LogError(ex, "Cannot delete file data - connected to a read-only Redis replica. File ID: {fileId}", fileId);
                throw new InvalidOperationException($"Cannot delete file data - connected to a read-only Redis replica. File ID: {fileId}", ex);
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Error while cleaning up file: {fileId}", fileId);
                throw;
            }
        }
    }
}