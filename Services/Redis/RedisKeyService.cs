using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace WorkoutTrackerWeb.Services.Redis
{
    /// <summary>
    /// Configuration options for Redis key management
    /// </summary>
    public class RedisKeyOptions
    {
        /// <summary>
        /// Default TTL for data that shouldn't live forever
        /// </summary>
        public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromHours(24);

        /// <summary>
        /// TTL for cached query results
        /// </summary>
        public TimeSpan QueryCacheExpiration { get; set; } = TimeSpan.FromHours(1);

        /// <summary>
        /// TTL for user session data
        /// </summary>
        public TimeSpan SessionExpiration { get; set; } = TimeSpan.FromHours(24);

        /// <summary>
        /// TTL for API rate limiting data
        /// </summary>
        public TimeSpan RateLimitExpiration { get; set; } = TimeSpan.FromMinutes(15);

        /// <summary>
        /// TTL for temporary files/uploads
        /// </summary>
        public TimeSpan FileExpiration { get; set; } = TimeSpan.FromDays(1);

        /// <summary>
        /// TTL for background job data
        /// </summary>
        public TimeSpan JobExpiration { get; set; } = TimeSpan.FromDays(7);

        /// <summary>
        /// TTL for auth tokens and verification data
        /// </summary>
        public TimeSpan TokenExpiration { get; set; } = TimeSpan.FromMinutes(15);

        /// <summary>
        /// Redis key namespace/instance prefix
        /// </summary>
        public string InstancePrefix { get; set; } = "wt:";

        /// <summary>
        /// Whether to enable key namespacing
        /// </summary>
        public bool EnableNamespacing { get; set; } = true;
    }

    /// <summary>
    /// Interface for Redis key management
    /// </summary>
    public interface IRedisKeyService
    {
        /// <summary>
        /// Generates a Redis key with the proper namespace and structure
        /// </summary>
        /// <param name="entityType">Type of entity (e.g., user, workout, session)</param>
        /// <param name="identifier">Entity identifier</param>
        /// <param name="subType">Sub-entity or property name (optional)</param>
        /// <returns>Formatted Redis key</returns>
        string CreateKey(string entityType, string identifier, string subType = null);

        /// <summary>
        /// Generates a Redis key with the proper namespace and structure
        /// </summary>
        /// <param name="entityType">Type of entity (e.g., user, workout, session)</param>
        /// <param name="identifier">Entity identifier as numeric</param>
        /// <param name="subType">Sub-entity or property name (optional)</param>
        /// <returns>Formatted Redis key</returns>
        string CreateKey(string entityType, int identifier, string subType = null);

        /// <summary>
        /// Generates a hash key from complex object parameters
        /// </summary>
        /// <param name="parameters">Object whose properties will be used for hash generation</param>
        /// <returns>A deterministic hash string</returns>
        string CreateHashFromObject(object parameters);

        /// <summary>
        /// Creates a key specifically for query caching
        /// </summary>
        /// <param name="queryName">Name/identifier of the query</param>
        /// <param name="parameters">Parameters that affect the query results</param>
        /// <returns>Query cache key</returns>
        string CreateQueryKey(string queryName, object parameters = null);

        /// <summary>
        /// Creates a key specifically for a user session
        /// </summary>
        /// <param name="sessionId">The session identifier</param>
        /// <returns>Session key</returns>
        string CreateSessionKey(string sessionId);

        /// <summary>
        /// Creates a key for file storage
        /// </summary>
        /// <param name="fileId">File identifier</param>
        /// <param name="subType">Sub-type (e.g., "meta", chunk index)</param>
        /// <returns>File storage key</returns>
        string CreateFileKey(string fileId, string subType = null);

        /// <summary>
        /// Gets the recommended TTL for a specific key type
        /// </summary>
        /// <param name="keyType">Type of key (query, session, file, etc)</param>
        /// <returns>Recommended TTL</returns>
        TimeSpan GetExpirationForKeyType(RedisKeyType keyType);
        
        /// <summary>
        /// Extracts the entity type from a Redis key
        /// </summary>
        /// <param name="key">Redis key</param>
        /// <returns>Entity type or null if not parseable</returns>
        string ExtractEntityTypeFromKey(string key);

        /// <summary>
        /// Get the pattern for searching keys of a specific entity type
        /// </summary>
        /// <param name="entityType">Type of entity</param>
        /// <returns>Redis key pattern for searching</returns>
        string GetKeyPatternForEntityType(string entityType);
    }

    /// <summary>
    /// Types of Redis keys with different TTL requirements
    /// </summary>
    public enum RedisKeyType
    {
        Default,
        Query,
        Session, 
        RateLimit,
        File,
        Job,
        Token,
        Permanent
    }

    /// <summary>
    /// Service that provides standardized Redis key generation and management
    /// </summary>
    public class RedisKeyService : IRedisKeyService
    {
        private readonly ILogger<RedisKeyService> _logger;
        private readonly RedisKeyOptions _options;

        public RedisKeyService(
            ILogger<RedisKeyService> logger,
            IOptions<RedisKeyOptions> options)
        {
            _logger = logger;
            _options = options.Value;
        }

        /// <inheritdoc/>
        public string CreateKey(string entityType, string identifier, string subType = null)
        {
            if (string.IsNullOrEmpty(entityType))
            {
                throw new ArgumentException("Entity type cannot be null or empty", nameof(entityType));
            }

            if (string.IsNullOrEmpty(identifier))
            {
                throw new ArgumentException("Identifier cannot be null or empty", nameof(identifier));
            }

            // Normalize components to lowercase with no spaces
            entityType = NormalizeKeyComponent(entityType);
            identifier = NormalizeKeyComponent(identifier);
            
            // Build the key with namespace prefix
            string key = _options.EnableNamespacing 
                ? $"{_options.InstancePrefix}{entityType}:{identifier}"
                : $"{entityType}:{identifier}";

            // Add optional sub-type
            if (!string.IsNullOrEmpty(subType))
            {
                subType = NormalizeKeyComponent(subType);
                key = $"{key}:{subType}";
            }

            _logger.LogTrace("Created Redis key: {Key}", key);
            return key;
        }

        /// <inheritdoc/>
        public string CreateKey(string entityType, int identifier, string subType = null)
        {
            return CreateKey(entityType, identifier.ToString(), subType);
        }

        /// <inheritdoc/>
        public string CreateHashFromObject(object parameters)
        {
            if (parameters == null)
            {
                return "default";
            }

            // Serialize the object to JSON to get a consistent string representation
            var parametersJson = System.Text.Json.JsonSerializer.Serialize(parameters);
            
            // Use a cryptographic hash for a fixed-length deterministic hash
            using (SHA256 sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(parametersJson));
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        /// <inheritdoc/>
        public string CreateQueryKey(string queryName, object parameters = null)
        {
            if (string.IsNullOrEmpty(queryName))
            {
                throw new ArgumentException("Query name cannot be null or empty", nameof(queryName));
            }

            queryName = NormalizeKeyComponent(queryName);
            string key = CreateKey("query", queryName);

            // Add parameters hash if provided
            if (parameters != null)
            {
                string hash = CreateHashFromObject(parameters);
                key = $"{key}:{hash}";
            }

            return key;
        }

        /// <inheritdoc/>
        public string CreateSessionKey(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));
            }

            return CreateKey("session", sessionId);
        }

        /// <inheritdoc/>
        public string CreateFileKey(string fileId, string subType = null)
        {
            if (string.IsNullOrEmpty(fileId))
            {
                throw new ArgumentException("File ID cannot be null or empty", nameof(fileId));
            }

            return CreateKey("file", fileId, subType);
        }

        /// <inheritdoc/>
        public TimeSpan GetExpirationForKeyType(RedisKeyType keyType)
        {
            return keyType switch
            {
                RedisKeyType.Query => _options.QueryCacheExpiration,
                RedisKeyType.Session => _options.SessionExpiration,
                RedisKeyType.RateLimit => _options.RateLimitExpiration,
                RedisKeyType.File => _options.FileExpiration,
                RedisKeyType.Job => _options.JobExpiration,
                RedisKeyType.Token => _options.TokenExpiration,
                RedisKeyType.Permanent => Timeout.InfiniteTimeSpan,
                RedisKeyType.Default => _options.DefaultExpiration,
                _ => _options.DefaultExpiration
            };
        }

        /// <inheritdoc/>
        public string ExtractEntityTypeFromKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            try
            {
                // Remove instance prefix if present
                string workKey = key;
                if (_options.EnableNamespacing && key.StartsWith(_options.InstancePrefix))
                {
                    workKey = key.Substring(_options.InstancePrefix.Length);
                }

                // Extract the entity type segment (first segment)
                int firstColonIndex = workKey.IndexOf(':');
                if (firstColonIndex > 0)
                {
                    return workKey.Substring(0, firstColonIndex);
                }

                return workKey; // No colon, the key is the entity type
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract entity type from key: {Key}", key);
                return null;
            }
        }

        /// <inheritdoc/>
        public string GetKeyPatternForEntityType(string entityType)
        {
            if (string.IsNullOrEmpty(entityType))
            {
                throw new ArgumentException("Entity type cannot be null or empty", nameof(entityType));
            }

            entityType = NormalizeKeyComponent(entityType);
            
            // Create a pattern for all keys of this entity type
            return _options.EnableNamespacing
                ? $"{_options.InstancePrefix}{entityType}:*"
                : $"{entityType}:*";
        }

        /// <summary>
        /// Normalizes a key component (removing spaces, lowercase)
        /// </summary>
        private string NormalizeKeyComponent(string component)
        {
            // Replace spaces with underscores, convert to lowercase
            return component
                .Replace(" ", "_")
                .Replace(".", "_")
                .ToLowerInvariant()
                .Trim();
        }
    }
}