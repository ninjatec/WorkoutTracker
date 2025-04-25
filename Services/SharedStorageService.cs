using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text;

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
        private const string KEY_PREFIX = "file:";
        private const string META_SUFFIX = ":meta";
        private const int CHUNK_SIZE = 1024 * 1024; // 1MB chunks
        private const int MAX_RETRIES = 3;
        private const int RETRY_DELAY_MS = 500;

        public RedisSharedStorageService(
            IConnectionMultiplexer redis,
            ILogger<RedisSharedStorageService> logger)
        {
            _redis = redis;
            _logger = logger;
        }

        // Helper method to get a database connection with write capability
        private IDatabase GetWritableDatabase()
        {
            if (_redis == null)
            {
                // In development mode or when Redis is configured as null, the local filesystem version is used
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
                        // Wait before retry with exponential backoff
                        int delayMs = baseDelayMs * (int)Math.Pow(2, retryCount);
                        Task.Delay(delayMs).Wait(); // Small delay before retry
                        retryCount++;
                        continue;
                    }

                    // Get the database with explicit write flag for master node
                    var db = _redis.GetDatabase(flags: CommandFlags.PreferMaster);
                    
                    // Identify master nodes explicitly
                    EndPoint masterEndpoint = null;
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

                    // Create a unique test key that won't interfere with real data
                    string testKey = $"redis:master:writetest:{Guid.NewGuid()}";
                    
                    // Set explicit command flags to always use master for write operations
                    var writeFlags = CommandFlags.PreferMaster | CommandFlags.DemandMaster;
                    
                    // Use SetAsync instead of StringSet to avoid blocking
                    var setTask = db.StringSetAsync(testKey, "test", TimeSpan.FromSeconds(5), flags: writeFlags);
                    
                    // Wait with timeout to avoid hanging indefinitely
                    if (!setTask.Wait(TimeSpan.FromSeconds(5)))
                    {
                        throw new TimeoutException("Redis write test timed out after 5 seconds");
                    }
                    
                    if (!setTask.Result)
                    {
                        throw new InvalidOperationException("Redis write test failed - SET operation returned false");
                    }
                    
                    // If we get here, the write was successful
                    return db;
                }
                catch (RedisConnectionException connEx)
                {
                    _logger.LogWarning(connEx, "Redis connection error. Attempt {RetryCount}/{MaxRetries}. Will retry.", 
                        retryCount + 1, maxRetries);
                    
                    // Wait before retry with exponential backoff
                    int delayMs = baseDelayMs * (int)Math.Pow(2, retryCount);
                    Task.Delay(delayMs).Wait();
                    retryCount++;
                }
                catch (RedisCommandException ex) when (ex.Message.Contains("READONLY") || ex.Message.Contains("replica"))
                {
                    _logger.LogWarning("Connected to a read-only Redis replica. Attempting to find writable master. Error: {Error}", ex.Message);
                    
                    try
                    {
                        // Force a reconnection attempt to find a master
                        var connection = _redis as ConnectionMultiplexer;
                        if (connection != null)
                        {
                            connection.Configure(); // Trigger configuration refresh
                        }
                        
                        // Wait before retry with exponential backoff
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
                        
                        // Wait before retry with exponential backoff
                        int delayMs = baseDelayMs * (int)Math.Pow(2, retryCount);
                        Task.Delay(delayMs).Wait();
                        retryCount++;
                    }
                    else
                    {
                        _logger.LogError(ex, "Error checking Redis write capability after {RetryCount} attempts", maxRetries);
                        
                        // Last resort: return the database even if we couldn't verify write capability
                        _logger.LogWarning("Returning Redis database connection without write verification. Operations may fail.");
                        return _redis.GetDatabase(flags: CommandFlags.PreferMaster);
                    }
                }
            }
            
            // If we exit the loop, we've failed all retries
            _logger.LogError("Failed to get a writable Redis connection after {MaxRetries} attempts", maxRetries);
            throw new InvalidOperationException($"Failed to get a writable Redis connection after {maxRetries} attempts");
        }

        public async Task<string> StoreFileAsync(Stream fileStream, string fileExtension = "", TimeSpan? expiry = null)
        {
            if (fileStream == null)
            {
                throw new ArgumentNullException(nameof(fileStream));
            }

            string fileId = $"{Guid.NewGuid()}";
            string fileKey = $"{KEY_PREFIX}{fileId}";
            string metaKey = $"{fileKey}{META_SUFFIX}";
            
            // Use GetWritableDatabase to ensure we're writing to a master, not a replica
            var db = GetWritableDatabase();
            
            // If we couldn't get a Redis database but Redis is configured, this is an error
            if (db == null && _redis != null)
            {
                _logger.LogError("Failed to get a writable Redis connection for storing file");
                throw new InvalidOperationException("Failed to get a writable Redis connection for storing file");
            }
            
            // If Redis is not configured, fall back to local filesystem
            if (db == null && _redis == null)
            {
                // We're in development mode with local filesystem storage
                _logger.LogWarning("Redis is not configured, falling back to local filesystem storage");
                // Create a temp file to simulate Redis storage in development
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
                    
                    // Store file metadata with retry
                    await db.HashSetAsync(metaKey, new HashEntry[]
                    {
                        new HashEntry("extension", fileExtension),
                        new HashEntry("created", DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                    });

                    // Apply expiry to metadata if specified
                    if (expiry.HasValue)
                    {
                        await db.KeyExpireAsync(metaKey, expiry.Value);
                    }

                    // Store file content in chunks for large files
                    byte[] buffer = new byte[CHUNK_SIZE];
                    int bytesRead;
                    long position = 0;

                    while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        if (bytesRead < buffer.Length)
                        {
                            Array.Resize(ref buffer, bytesRead);
                        }

                        // Use retry for each chunk
                        bool chunkStored = false;
                        int chunkRetryCount = 0;
                        
                        while (!chunkStored && chunkRetryCount < 3)
                        {
                            try 
                            {
                                string chunkKey = $"{fileKey}:{position}";
                                await db.StringSetAsync(chunkKey, buffer);
                                
                                // Apply expiry to chunk if specified
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

                    // Store total size in metadata
                    await db.HashSetAsync(metaKey, "size", position);

                    _logger.LogInformation("File stored successfully in shared storage. ID: {FileId}, Size: {Size} bytes", fileId, position);
                    return fileId;
                }
                catch (RedisCommandException ex) when (ex.Message.Contains("READONLY"))
                {
                    retryCount++;
                    if (retryCount >= maxRetries)
                    {
                        _logger.LogError(ex, "Cannot store file - connected to a read-only Redis replica. File ID: {FileId}", fileId);
                        
                        // Try to clean up any partial data that may have been created
                        try { await CleanupFileAsync(fileId); } catch { /* Ignore cleanup errors */ }
                        
                        throw new InvalidOperationException($"Cannot store file - connected to a read-only Redis replica. File ID: {fileId}", ex);
                    }
                    
                    _logger.LogWarning(ex, "Connected to a read-only Redis replica. Attempt {RetryCount}/{MaxRetries}", 
                        retryCount, maxRetries);
                        
                    // Try to get a writable connection again
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
                        
                        // Try to clean up any partial data that may have been created
                        try { await CleanupFileAsync(fileId); } catch { /* Ignore cleanup errors */ }
                        
                        throw;
                    }
                    
                    _logger.LogWarning(connEx, "Redis connection issue. Attempt {RetryCount}/{MaxRetries}", 
                        retryCount, maxRetries);
                    
                    // Try to get a writable connection again
                    db = GetWritableDatabase();
                    
                    await Task.Delay(retryDelayMs * retryCount);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error storing file in shared storage: {Message}", ex.Message);
                    
                    // Clean up any chunks that may have been created
                    try { await CleanupFileAsync(fileId); } catch { /* Ignore cleanup errors */ }
                    
                    throw;
                }
            }
            
            // This should not be reached due to the exception in the last retry, but just in case
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

            string fileKey = $"{KEY_PREFIX}{fileId}";
            string metaKey = $"{fileKey}{META_SUFFIX}";
            var db = _redis.GetDatabase();

            // Check if file exists
            if (!await db.KeyExistsAsync(metaKey))
            {
                _logger.LogWarning("File not found in shared storage: {FileId}", fileId);
                throw new FileNotFoundException($"File not found in shared storage: {fileId}");
            }

            try
            {
                // Get metadata
                var metaHash = await db.HashGetAllAsync(metaKey);
                var meta = metaHash.ToDictionary(h => (string)h.Name, h => (string)h.Value);
                
                if (!meta.TryGetValue("size", out string sizeStr) || !long.TryParse(sizeStr, out long size))
                {
                    throw new InvalidDataException($"Invalid file metadata for {fileId}");
                }
                
                string extension = meta.TryGetValue("extension", out string ext) ? ext : "";

                // Create memory stream to hold file content
                MemoryStream memoryStream = new MemoryStream();
                
                // Retrieve chunks and write to memory stream
                long position = 0;
                while (position < size)
                {
                    string chunkKey = $"{fileKey}:{position}";
                    var chunk = await db.StringGetAsync(chunkKey);
                    
                    if (chunk.IsNull)
                    {
                        throw new InvalidDataException($"Missing chunk at position {position} for file {fileId}");
                    }
                    
                    // Convert the RedisValue to byte array before writing
                    byte[] bytes = chunk;
                    await memoryStream.WriteAsync(bytes, 0, bytes.Length);
                    position += bytes.Length;
                }
                
                // Reset position to start
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
                // Get the file from shared storage
                var (fileStream, extension) = await RetrieveFileAsync(fileId);
                
                // Create a temporary file with the correct extension
                string tempDir = Path.GetTempPath();
                string tempFileName = $"{Path.GetRandomFileName()}{extension}";
                string tempFilePath = Path.Combine(tempDir, tempFileName);
                
                // Write the file to disk
                using (var fileStream2 = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
                {
                    await fileStream.CopyToAsync(fileStream2);
                }
                
                // Clean up the memory stream
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

            try {
                await CleanupFileAsync(fileId);
                _logger.LogInformation("File deleted from shared storage: {FileId}", fileId);
            }
            catch (RedisCommandException ex) when (ex.Message.Contains("READONLY"))
            {
                _logger.LogError(ex, "Cannot delete file - connected to a read-only Redis replica. File ID: {FileId}", fileId);
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

            string metaKey = $"{KEY_PREFIX}{fileId}{META_SUFFIX}";
            var db = _redis.GetDatabase();
            
            return await db.KeyExistsAsync(metaKey);
        }

        private async Task CleanupFileAsync(string fileId)
        {
            string fileKey = $"{KEY_PREFIX}{fileId}";
            string metaKey = $"{fileKey}{META_SUFFIX}";
            var db = GetWritableDatabase();
            
            // Early exit if we failed to get a writable database connection
            if (db == null && _redis != null)
            {
                _logger.LogWarning("Could not get a writable Redis connection for cleanup. File ID: {FileId}", fileId);
                return;
            }
            
            try {
                // Get size to know how many chunks to delete
                HashEntry[] metaEntries = await db.HashGetAllAsync(metaKey);
                var meta = metaEntries.ToDictionary(h => (string)h.Name, h => (string)h.Value);
                
                if (meta.TryGetValue("size", out string sizeStr) && long.TryParse(sizeStr, out long size))
                {
                    // Delete all chunks
                    long position = 0;
                    while (position < size)
                    {
                        string chunkKey = $"{fileKey}:{position}";
                        RedisValue chunk = RedisValue.Null;
                        
                        try
                        {
                            // Use a timeout for the get operation to prevent hanging
                            var getTask = db.StringGetAsync(chunkKey);
                            if (await Task.WhenAny(getTask, Task.Delay(5000)) == getTask)
                            {
                                chunk = await getTask;
                            }
                            else
                            {
                                _logger.LogWarning("Timeout getting chunk at position {Position} for file {FileId}", position, fileId);
                                // Move to next chunk to avoid getting stuck
                                position += CHUNK_SIZE;
                                continue;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error getting chunk at position {Position} for file {FileId}", position, fileId);
                            // Move to next chunk to avoid getting stuck
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
                                _logger.LogWarning(ex, "Error deleting chunk at position {Position} for file {FileId}", position, fileId);
                                // Move to next chunk to avoid getting stuck
                                position += CHUNK_SIZE;
                            }
                        }
                        else
                        {
                            // If chunk is missing, assume fixed size and move to next one
                            position += CHUNK_SIZE;
                        }
                    }
                }
                else
                {
                    // If we can't get size, try to delete chunks using a pattern search
                    try
                    {
                        if (_redis.GetEndPoints().Length > 0)
                        {
                            var server = _redis.GetServer(_redis.GetEndPoints()[0]);
                            
                            // Use a timeout for the KEYS operation to prevent hanging
                            var keysTask = Task.Run(() => server.Keys(pattern: $"{fileKey}:*").ToArray());
                            RedisKey[] keys;
                            
                            if (await Task.WhenAny(keysTask, Task.Delay(10000)) == keysTask)
                            {
                                keys = await keysTask;
                                
                                foreach (var key in keys)
                                {
                                    try 
                                    {
                                        await db.KeyDeleteAsync(key);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning(ex, "Error deleting key {Key} for file {FileId}", key, fileId);
                                    }
                                }
                            }
                            else
                            {
                                _logger.LogWarning("Timeout searching for chunk keys for file {FileId}", fileId);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error searching for chunk keys for file {FileId}", fileId);
                    }
                }
                
                // Delete metadata as the last step
                try
                {
                    await db.KeyDeleteAsync(metaKey);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error deleting metadata for file {FileId}", fileId);
                }
            }
            catch (RedisCommandException ex) when (ex.Message.Contains("READONLY"))
            {
                _logger.LogError(ex, "Cannot delete file data - connected to a read-only Redis replica. File ID: {FileId}", fileId);
                throw new InvalidOperationException($"Cannot delete file data - connected to a read-only Redis replica. File ID: {fileId}", ex);
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Error while cleaning up file: {FileId}", fileId);
                throw;
            }
        }
    }
}