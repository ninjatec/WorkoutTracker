using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;
using System.Text;

namespace WorkoutTrackerWeb.Services.Redis
{
    public interface IResilientCacheService
    {
        Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration, bool slidingExpiration = false) where T : class;
        Task<(bool Found, T Value)> TryGetValueAsync<T>(string key) where T : class;
        Task SetAsync<T>(string key, T value, TimeSpan expiration, bool slidingExpiration = false) where T : class;
        Task RemoveAsync(string key);
    }
    
    public class ResilientCacheService : IResilientCacheService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<ResilientCacheService> _logger;

        public ResilientCacheService(
            IDistributedCache cache, 
            ILogger<ResilientCacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration, bool slidingExpiration = false) where T : class
        {
            try
            {
                var cachedBytes = await _cache.GetAsync(key);
                if (cachedBytes != null)
                {
                    _logger.LogDebug("Cache hit for key {Key}", key);
                    var cachedValue = Deserialize<T>(cachedBytes);
                    if (cachedValue != null)
                    {
                        return cachedValue;
                    }
                    _logger.LogWarning("Failed to deserialize cached value for key {Key}", key);
                }

                _logger.LogDebug("Cache miss for key {Key}, generating value", key);
                var value = await factory();
                
                if (value != null)
                {
                    await SetAsync(key, value, expiration, slidingExpiration);
                }
                
                return value;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve or create value from cache for key {Key}", key);
                return await factory();
            }
        }

        public async Task<(bool Found, T Value)> TryGetValueAsync<T>(string key) where T : class
        {
            try
            {
                var cachedBytes = await _cache.GetAsync(key);
                if (cachedBytes != null)
                {
                    var value = Deserialize<T>(cachedBytes);
                    return (value != null, value);
                }
                return (false, default);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get value from cache for key {Key}", key);
                return (false, default);
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan expiration, bool slidingExpiration = false) where T : class
        {
            try
            {
                var options = new DistributedCacheEntryOptions();
                
                if (slidingExpiration)
                {
                    options.SlidingExpiration = expiration;
                }
                else
                {
                    options.AbsoluteExpirationRelativeToNow = expiration;
                }

                var serializedValue = Serialize(value);
                await _cache.SetAsync(key, serializedValue, options);
                _logger.LogDebug("Successfully cached value for key {Key} with expiration {Expiration}", key, expiration);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to set cache value for key {Key}", key);
                // We'll just swallow the exception and continue without caching
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                await _cache.RemoveAsync(key);
                _logger.LogDebug("Removed cache entry for key {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove cache entry for key {Key}", key);
            }
        }

        private byte[] Serialize<T>(T value)
        {
            var jsonString = JsonSerializer.Serialize(value);
            return Encoding.UTF8.GetBytes(jsonString);
        }

        private T Deserialize<T>(byte[] bytes)
        {
            var jsonString = Encoding.UTF8.GetString(bytes);
            return JsonSerializer.Deserialize<T>(jsonString);
        }
    }
    
    public class FallbackCacheService : IResilientCacheService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<FallbackCacheService> _logger;

        public FallbackCacheService(
            IDistributedCache cache,
            ILogger<FallbackCacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration, bool slidingExpiration = false) where T : class
        {
            try
            {
                // First try to get from memory cache
                var cachedBytes = await _cache.GetAsync(key);
                if (cachedBytes != null)
                {
                    _logger.LogDebug("Memory cache hit for key {Key}", key);
                    var cachedValue = Deserialize<T>(cachedBytes);
                    if (cachedValue != null)
                    {
                        return cachedValue;
                    }
                }

                // If not in cache or deserialization failed, create value
                _logger.LogDebug("Memory cache miss for key {Key}, generating value", key);
                var value = await factory();
                
                if (value != null)
                {
                    // Store in memory cache with shorter timeframe to avoid memory pressure
                    var shorterExpiration = TimeSpan.FromMinutes(Math.Min(expiration.TotalMinutes, 5));
                    await SetAsync(key, value, shorterExpiration, slidingExpiration);
                }
                
                return value;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve or create value from memory cache for key {Key}", key);
                return await factory();
            }
        }

        public async Task<(bool Found, T Value)> TryGetValueAsync<T>(string key) where T : class
        {
            try
            {
                var cachedBytes = await _cache.GetAsync(key);
                if (cachedBytes != null)
                {
                    var value = Deserialize<T>(cachedBytes);
                    return (value != null, value);
                }
                return (false, default);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get value from memory cache for key {Key}", key);
                return (false, default);
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan expiration, bool slidingExpiration = false) where T : class
        {
            try
            {
                var options = new DistributedCacheEntryOptions();
                
                // Limit memory cache expiration to 5 minutes to avoid memory pressure
                var safeDuration = TimeSpan.FromMinutes(Math.Min(expiration.TotalMinutes, 5));
                
                if (slidingExpiration)
                {
                    options.SlidingExpiration = safeDuration;
                }
                else
                {
                    options.AbsoluteExpirationRelativeToNow = safeDuration;
                }

                var serializedValue = Serialize(value);
                await _cache.SetAsync(key, serializedValue, options);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to set memory cache value for key {Key}", key);
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                await _cache.RemoveAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove memory cache entry for key {Key}", key);
            }
        }

        private byte[] Serialize<T>(T value)
        {
            var jsonString = JsonSerializer.Serialize(value);
            return Encoding.UTF8.GetBytes(jsonString);
        }

        private T Deserialize<T>(byte[] bytes)
        {
            var jsonString = Encoding.UTF8.GetString(bytes);
            return JsonSerializer.Deserialize<T>(jsonString);
        }
    }
}