using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WorkoutTrackerWeb.Services.Redis;
using System.Collections.Concurrent;
using System.Text;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Linq;

namespace WorkoutTrackerWeb.Services.Cache
{
    /// <summary>
    /// Interface for query result caching
    /// </summary>
    public interface IQueryResultCacheService
    {
        /// <summary>
        /// Gets a cached query result or generates it if not found
        /// </summary>
        /// <typeparam name="T">The type of the query result</typeparam>
        /// <param name="cacheKey">The cache key to use</param>
        /// <param name="queryFunc">The function to generate the query result if not found in cache</param>
        /// <param name="expiration">How long to keep the result in cache</param>
        /// <param name="slidingExpiration">Whether to use sliding expiration (reset timer on access)</param>
        /// <returns>The cached or newly generated query result</returns>
        Task<T> GetOrCreateQueryResultAsync<T>(string cacheKey, Func<Task<T>> queryFunc, TimeSpan expiration, bool slidingExpiration = false) where T : class;
        
        /// <summary>
        /// Gets a cached query result or generates it if not found, with automatic key generation
        /// </summary>
        /// <typeparam name="T">The type of the query result</typeparam>
        /// <param name="keyPrefix">Prefix for the cache key</param>
        /// <param name="queryParameters">Parameters to include in the cache key generation</param>
        /// <param name="queryFunc">The function to generate the query result if not found in cache</param>
        /// <param name="expiration">How long to keep the result in cache</param>
        /// <param name="slidingExpiration">Whether to use sliding expiration (reset timer on access)</param>
        /// <returns>The cached or newly generated query result</returns>
        Task<T> GetOrCreateQueryResultAsync<T>(string keyPrefix, object queryParameters, Func<Task<T>> queryFunc, TimeSpan expiration, bool slidingExpiration = false) where T : class;
        
        /// <summary>
        /// Tries to get a cached query result without generating it
        /// </summary>
        /// <typeparam name="T">The type of the query result</typeparam>
        /// <param name="cacheKey">The cache key to use</param>
        /// <returns>A tuple indicating whether the value was found and the value itself</returns>
        Task<(bool Found, T Value)> TryGetQueryResultAsync<T>(string cacheKey) where T : class;
        
        /// <summary>
        /// Manually caches a query result
        /// </summary>
        /// <typeparam name="T">The type of the query result</typeparam>
        /// <param name="cacheKey">The cache key to use</param>
        /// <param name="value">The value to cache</param>
        /// <param name="expiration">How long to keep the result in cache</param>
        /// <param name="slidingExpiration">Whether to use sliding expiration (reset timer on access)</param>
        Task CacheQueryResultAsync<T>(string cacheKey, T value, TimeSpan expiration, bool slidingExpiration = false) where T : class;
        
        /// <summary>
        /// Invalidates a cache entry for a specific key
        /// </summary>
        /// <param name="cacheKey">The cache key to invalidate</param>
        Task InvalidateQueryResultAsync(string cacheKey);
        
        /// <summary>
        /// Invalidates all cache entries with a specific prefix
        /// </summary>
        /// <param name="keyPrefix">The prefix of cache keys to invalidate</param>
        Task InvalidateQueryResultsByPrefixAsync(string keyPrefix);
        
        /// <summary>
        /// Generates a cache key from an object's properties
        /// </summary>
        /// <param name="keyPrefix">Prefix for the cache key</param>
        /// <param name="parameters">Object whose properties will be used for key generation</param>
        /// <returns>A deterministic cache key</returns>
        string GenerateCacheKey(string keyPrefix, object parameters);
    }
    
    /// <summary>
    /// Configuration options for QueryResultCacheService
    /// </summary>
    public class QueryResultCacheOptions
    {
        /// <summary>
        /// Default expiration for cached query results (1 hour)
        /// </summary>
        public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromHours(1);
        
        /// <summary>
        /// Whether to use sliding expiration by default (reset timer on access)
        /// </summary>
        public bool DefaultSlidingExpiration { get; set; } = false;
        
        /// <summary>
        /// Key prefix for all query cache entries
        /// </summary>
        public string GlobalKeyPrefix { get; set; } = "query:";
        
        /// <summary>
        /// Whether caching is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;
    }
    
    /// <summary>
    /// Service for caching database query results with Redis/in-memory fallback
    /// </summary>
    public class QueryResultCacheService : IQueryResultCacheService
    {
        private readonly IResilientCacheService _cacheService;
        private readonly ILogger<QueryResultCacheService> _logger;
        private readonly QueryResultCacheOptions _options;
        private readonly IRedisKeyService _keyService;
        
        // In-memory collection of cache key prefixes for tracking invalidation patterns
        private static readonly ConcurrentDictionary<string, HashSet<string>> _prefixRegistry = new();
        
        public QueryResultCacheService(
            IResilientCacheService cacheService,
            ILogger<QueryResultCacheService> logger,
            IOptions<QueryResultCacheOptions> options,
            IRedisKeyService keyService)
        {
            _cacheService = cacheService;
            _logger = logger;
            _options = options.Value;
            _keyService = keyService;
        }
        
        /// <inheritdoc />
        public async Task<T> GetOrCreateQueryResultAsync<T>(string cacheKey, Func<Task<T>> queryFunc, TimeSpan expiration, bool slidingExpiration = false) where T : class
        {
            if (!_options.Enabled)
            {
                _logger.LogDebug("Query caching is disabled. Executing query directly for key: {CacheKey}", cacheKey);
                return await queryFunc();
            }
            
            // Use the standardized Redis key service for key generation
            string fullCacheKey = _keyService.CreateQueryKey(cacheKey);
            
            try
            {
                // Record operation timing
                using (CacheMetrics.TimeOperation("get_or_create"))
                {
                    // Register this key with its prefix for invalidation tracking
                    RegisterKeyWithPrefix(fullCacheKey);
                    
                    var (found, value) = await _cacheService.TryGetValueAsync<T>(fullCacheKey);
                    
                    // Extract prefix for metrics recording
                    string prefix = _keyService.ExtractEntityTypeFromKey(fullCacheKey);
                    
                    if (found && value != null)
                    {
                        // Cache hit - record metric
                        CacheMetrics.RecordHit(prefix);
                        _logger.LogDebug("Cache hit for key: {CacheKey}", fullCacheKey);
                        return value;
                    }
                    
                    // Cache miss - record metric
                    CacheMetrics.RecordMiss(prefix);
                    _logger.LogDebug("Cache miss for key: {CacheKey}", fullCacheKey);
                    
                    // Execute the query and cache the result
                    var stopwatch = Stopwatch.StartNew();
                    T result = await queryFunc();
                    stopwatch.Stop();
                    
                    if (result != null)
                    {
                        await _cacheService.SetAsync(fullCacheKey, result, expiration, slidingExpiration);
                        CacheMetrics.IncrementSize(prefix);
                        _logger.LogDebug("Cached query result for key: {CacheKey}, took: {QueryTime:N0}ms", 
                            fullCacheKey, stopwatch.ElapsedMilliseconds);
                    }
                    
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error retrieving cached query result for key {CacheKey}. Executing query directly.", fullCacheKey);
                return await queryFunc();
            }
        }
        
        /// <inheritdoc />
        public async Task<T> GetOrCreateQueryResultAsync<T>(string keyPrefix, object queryParameters, Func<Task<T>> queryFunc, TimeSpan expiration, bool slidingExpiration = false) where T : class
        {
            // Use the RedisKeyService to generate the query key with parameters
            string cacheKey = _keyService.CreateQueryKey(keyPrefix, queryParameters);
            return await GetOrCreateQueryResultAsync(cacheKey, queryFunc, expiration, slidingExpiration);
        }
        
        /// <inheritdoc />
        public async Task<(bool Found, T Value)> TryGetQueryResultAsync<T>(string cacheKey) where T : class
        {
            if (!_options.Enabled)
            {
                return (false, null);
            }
            
            // Use the standardized Redis key service for key generation
            string fullCacheKey = _keyService.CreateQueryKey(cacheKey);
            
            try
            {
                // Record operation timing
                using (CacheMetrics.TimeOperation("try_get"))
                {
                    var result = await _cacheService.TryGetValueAsync<T>(fullCacheKey);
                    
                    // Extract prefix for metrics recording
                    string prefix = _keyService.ExtractEntityTypeFromKey(fullCacheKey);
                    
                    if (result.Found && result.Value != null)
                    {
                        CacheMetrics.RecordHit(prefix);
                    }
                    else
                    {
                        CacheMetrics.RecordMiss(prefix);
                    }
                    
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error retrieving cached query result for key {CacheKey}", fullCacheKey);
                return (false, null);
            }
        }
        
        /// <inheritdoc />
        public async Task CacheQueryResultAsync<T>(string cacheKey, T value, TimeSpan expiration, bool slidingExpiration = false) where T : class
        {
            if (!_options.Enabled || value == null)
            {
                return;
            }
            
            // Use the standardized Redis key service for key generation
            string fullCacheKey = _keyService.CreateQueryKey(cacheKey);
            
            try
            {
                // Record operation timing
                using (CacheMetrics.TimeOperation("set"))
                {
                    // Register this key with its prefix for invalidation tracking
                    RegisterKeyWithPrefix(fullCacheKey);
                    
                    await _cacheService.SetAsync(fullCacheKey, value, expiration, slidingExpiration);
                    
                    // Extract prefix for metrics recording
                    string prefix = _keyService.ExtractEntityTypeFromKey(fullCacheKey);
                    CacheMetrics.IncrementSize(prefix);
                    
                    _logger.LogDebug("Cached query result for key: {CacheKey} with expiration: {Expiration}", 
                        fullCacheKey, expiration);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error caching query result for key {CacheKey}", fullCacheKey);
            }
        }
        
        /// <inheritdoc />
        public async Task InvalidateQueryResultAsync(string cacheKey)
        {
            if (!_options.Enabled)
            {
                return;
            }
            
            // Use the standardized Redis key service for key generation
            string fullCacheKey = _keyService.CreateQueryKey(cacheKey);
            
            try
            {
                // Record operation timing
                using (CacheMetrics.TimeOperation("invalidate"))
                {
                    await _cacheService.RemoveAsync(fullCacheKey);
                    
                    // Extract prefix for metrics recording
                    string prefix = _keyService.ExtractEntityTypeFromKey(fullCacheKey);
                    CacheMetrics.DecrementSize(prefix);
                    CacheMetrics.RecordInvalidation(prefix, "manual");
                    
                    _logger.LogDebug("Invalidated cache entry for key: {CacheKey}", fullCacheKey);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error invalidating cache entry for key {CacheKey}", fullCacheKey);
            }
        }
        
        /// <inheritdoc />
        public async Task InvalidateQueryResultsByPrefixAsync(string keyPrefix)
        {
            if (!_options.Enabled || string.IsNullOrEmpty(keyPrefix))
            {
                return;
            }
            
            // Generate the pattern for keys with this prefix
            string fullPrefix = _keyService.CreateQueryKey(keyPrefix);
            
            try
            {
                // Record operation timing
                using (CacheMetrics.TimeOperation("invalidate_prefix"))
                {
                    // Get all keys registered with this prefix
                    if (_prefixRegistry.TryGetValue(fullPrefix, out var registeredKeys))
                    {
                        foreach (var key in registeredKeys.ToArray())
                        {
                            await _cacheService.RemoveAsync(key);
                        }
                        
                        _logger.LogDebug("Invalidated {Count} cache entries with prefix: {Prefix}", 
                            registeredKeys.Count, fullPrefix);
                            
                        // Record metrics for this invalidation
                        string normalizedPrefix = _keyService.ExtractEntityTypeFromKey(fullPrefix);
                        CacheMetrics.RecordInvalidation(normalizedPrefix, "entity_change");
                    }
                    else
                    {
                        _logger.LogDebug("No cache entries found with prefix: {Prefix}", fullPrefix);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error invalidating cache entries with prefix {Prefix}", fullPrefix);
            }
        }
        
        /// <inheritdoc />
        public string GenerateCacheKey(string keyPrefix, object parameters)
        {
            // Delegate to the RedisKeyService for consistent key generation
            return _keyService.CreateQueryKey(keyPrefix, parameters);
        }
        
        #region Private Helper Methods
        
        private void RegisterKeyWithPrefix(string fullCacheKey)
        {
            // Extract prefix using the key service
            string prefix = _keyService.ExtractEntityTypeFromKey(fullCacheKey) + ":";
                
            // Register this key with its prefix
            _prefixRegistry.AddOrUpdate(
                prefix,
                _ => new HashSet<string> { fullCacheKey },
                (_, existingSet) =>
                {
                    lock (existingSet)
                    {
                        existingSet.Add(fullCacheKey);
                        return existingSet;
                    }
                });
        }
        
        #endregion
    }
}