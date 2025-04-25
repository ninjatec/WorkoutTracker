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

        public RedisSharedStorageService(
            IConnectionMultiplexer redis,
            ILogger<RedisSharedStorageService> logger)
        {
            _redis = redis;
            _logger = logger;
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
            var db = _redis.GetDatabase();

            try
            {
                _logger.LogInformation("Storing file in shared storage with ID: {FileId}", fileId);
                
                // Store file metadata
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

                    string chunkKey = $"{fileKey}:{position}";
                    await db.StringSetAsync(chunkKey, buffer);
                    
                    // Apply expiry to chunk if specified
                    if (expiry.HasValue)
                    {
                        await db.KeyExpireAsync(chunkKey, expiry.Value);
                    }

                    position += bytesRead;
                }

                // Store total size in metadata
                await db.HashSetAsync(metaKey, "size", position);

                _logger.LogInformation("File stored successfully in shared storage. ID: {FileId}, Size: {Size} bytes", fileId, position);
                return fileId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing file in shared storage: {Message}", ex.Message);
                
                // Clean up any chunks that may have been created
                await CleanupFileAsync(fileId);
                
                throw;
            }
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

            await CleanupFileAsync(fileId);
            _logger.LogInformation("File deleted from shared storage: {FileId}", fileId);
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
            var db = _redis.GetDatabase();
            
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
                    var chunk = await db.StringGetAsync(chunkKey);
                    
                    if (!chunk.IsNull)
                    {
                        await db.KeyDeleteAsync(chunkKey);
                        // Convert RedisValue to byte array to get the length
                        byte[] bytes = chunk;
                        position += bytes.Length;
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
                var server = _redis.GetServer(_redis.GetEndPoints()[0]);
                foreach (var key in server.Keys(pattern: $"{fileKey}:*"))
                {
                    await db.KeyDeleteAsync(key);
                }
            }
            
            // Delete metadata
            await db.KeyDeleteAsync(metaKey);
        }
    }
}