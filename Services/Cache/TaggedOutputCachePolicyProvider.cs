using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Threading;

namespace WorkoutTrackerWeb.Services.Cache
{
    /// <summary>
    /// Output cache policy provider that supports tagged cache entries,
    /// which allows for more granular cache invalidation
    /// </summary>
    public class TaggedOutputCachePolicyProvider : ITaggedOutputCachePolicyProvider
    {
        private readonly ILogger<TaggedOutputCachePolicyProvider> _logger;
        
        public TaggedOutputCachePolicyProvider(
            ILogger<TaggedOutputCachePolicyProvider> logger)
        {
            _logger = logger;
        }
        
        /// <summary>
        /// Create a policy with the specified tags
        /// </summary>
        public IEnumerable<string> GetCacheTags(HttpContext context)
        {
            // Check if this is a request for one of our custom tagged policies
            if (context.Items.TryGetValue("OutputCache_Tags", out var tagsObj) && 
                tagsObj is HashSet<string> tags)
            {
                _logger.LogDebug("Creating tagged output cache policy with {TagCount} tags", tags.Count);
                return tags;
            }
            
            // If no tags found, return empty
            return Array.Empty<string>();
        }
    }
    
    /// <summary>
    /// Interface for custom output cache policy provider with tag support
    /// </summary>
    public interface ITaggedOutputCachePolicyProvider
    {
        /// <summary>
        /// Get the tags for a cache entry based on the HttpContext
        /// </summary>
        IEnumerable<string> GetCacheTags(HttpContext context);
    }
    
    /// <summary>
    /// Extension methods for OutputCache integration
    /// </summary>
    public static class OutputCacheExtensions
    {
        /// <summary>
        /// Add tags to the current request that will be used for cache invalidation
        /// </summary>
        /// <param name="context">The HttpContext instance</param>
        /// <param name="tags">The cache tags to add</param>
        public static void AddOutputCacheTags(this HttpContext context, params string[] tags)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (tags == null || tags.Length == 0) return;

            // Get or create the tags collection in HttpContext.Items
            if (!context.Items.TryGetValue("OutputCache_Tags", out var existingTagsObj) || 
                !(existingTagsObj is HashSet<string> existingTags))
            {
                existingTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                context.Items["OutputCache_Tags"] = existingTags;
            }
            
            // Add the new tags
            foreach (var tag in tags)
            {
                if (!string.IsNullOrWhiteSpace(tag))
                {
                    existingTags.Add(tag);
                }
            }
        }
    }
    
    /// <summary>
    /// Manages anti-dogpile locking for high-concurrency cache scenarios
    /// </summary>
    public class AntiDogpileLockManager
    {
        private static readonly Dictionary<string, SemaphoreSlim> _locks = new();
        private static readonly SemaphoreSlim _locksLock = new(1, 1);
        private readonly ILogger<AntiDogpileLockManager> _logger;
        
        public AntiDogpileLockManager(ILogger<AntiDogpileLockManager> logger = null)
        {
            _logger = logger;
        }
        
        /// <summary>
        /// Try to acquire a lock for a specific cache key
        /// </summary>
        /// <param name="cacheKey">The cache key to lock</param>
        /// <param name="timeout">How long to wait for the lock</param>
        /// <param name="cancellation">Cancellation token</param>
        /// <returns>A disposable lock handle that releases the lock when disposed</returns>
        public async Task<IDisposable> AcquireLockAsync(string cacheKey, TimeSpan timeout, CancellationToken cancellation = default)
        {
            var keyLock = await GetKeyLockAsync(cacheKey);
            
            if (await keyLock.WaitAsync(timeout, cancellation))
            {
                return new LockHandle(keyLock, () => 
                {
                    _logger?.LogDebug("Released lock for cache key: {CacheKey}", cacheKey);
                });
            }
            
            _logger?.LogWarning("Timed out waiting for cache lock on key: {CacheKey}", cacheKey);
            return null; // Couldn't acquire lock
        }
        
        private async Task<SemaphoreSlim> GetKeyLockAsync(string cacheKey)
        {
            // Fast path if lock already exists
            if (_locks.TryGetValue(cacheKey, out var keyLock))
            {
                return keyLock;
            }
            
            // Slow path: create new lock
            await _locksLock.WaitAsync();
            try
            {
                if (!_locks.TryGetValue(cacheKey, out keyLock))
                {
                    keyLock = new SemaphoreSlim(1, 1);
                    _locks[cacheKey] = keyLock;
                }
                return keyLock;
            }
            finally
            {
                _locksLock.Release();
            }
        }
        
        /// <summary>
        /// Cleanup method to remove expired locks
        /// </summary>
        public void CleanupExpiredLocks()
        {
            // In a real application, we'd need a mechanism to know which locks are no longer needed
            // For simplicity, this is left as an exercise. In production, we would track when locks
            // were last used and clean up periodically.
        }
        
        private class LockHandle : IDisposable
        {
            private readonly SemaphoreSlim _semaphore;
            private readonly Action _onDispose;
            private bool _disposed = false;
            
            public LockHandle(SemaphoreSlim semaphore, Action onDispose = null)
            {
                _semaphore = semaphore;
                _onDispose = onDispose;
            }
            
            public void Dispose()
            {
                if (_disposed) return;
                
                _onDispose?.Invoke();
                _semaphore.Release();
                _disposed = true;
            }
        }
    }
}