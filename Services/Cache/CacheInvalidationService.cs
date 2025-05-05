using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Linq;

namespace WorkoutTrackerWeb.Services.Cache
{
    /// <summary>
    /// Service to handle cache invalidation based on entity changes
    /// </summary>
    public interface ICacheInvalidationService
    {
        /// <summary>
        /// Register an entity type for cache invalidation
        /// </summary>
        /// <typeparam name="TEntity">The entity type to register</typeparam>
        /// <param name="keyPrefix">The cache key prefix to invalidate when this entity changes</param>
        void RegisterEntityType<TEntity>(string keyPrefix) where TEntity : class;
        
        /// <summary>
        /// Register multiple entity types for cache invalidation
        /// </summary>
        /// <param name="entityTypeToPrefixMap">Dictionary mapping entity types to cache prefixes</param>
        void RegisterEntityTypes(Dictionary<Type, string> entityTypeToPrefixMap);
        
        /// <summary>
        /// Process database changes and invalidate related caches
        /// </summary>
        /// <param name="entries">Changed entity entries from DbContext.ChangeTracker</param>
        /// <returns>Task representing the async operation</returns>
        Task ProcessChangesAsync(IEnumerable<EntityEntry> entries);
        
        /// <summary>
        /// Invalidate all caches for a specific entity type
        /// </summary>
        /// <typeparam name="TEntity">The entity type whose caches should be invalidated</typeparam>
        /// <returns>Task representing the async operation</returns>
        Task InvalidateEntityCacheAsync<TEntity>() where TEntity : class;
        
        /// <summary>
        /// Manually invalidate a cache for a specific entity instance
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <param name="entity">The entity instance</param>
        /// <param name="action">The change action (Add/Update/Delete)</param>
        /// <returns>Task representing the async operation</returns>
        Task InvalidateEntityCacheAsync<TEntity>(TEntity entity, EntityState action) where TEntity : class;
    }
    
    /// <summary>
    /// Implementation of cache invalidation service for database changes
    /// </summary>
    public class CacheInvalidationService : ICacheInvalidationService
    {
        private readonly IQueryResultCacheService _queryCache;
        private readonly ILogger<CacheInvalidationService> _logger;
        private readonly Dictionary<Type, List<string>> _entityPrefixMap = new();
        
        public CacheInvalidationService(
            IQueryResultCacheService queryCache,
            ILogger<CacheInvalidationService> logger)
        {
            _queryCache = queryCache;
            _logger = logger;
        }
        
        /// <inheritdoc />
        public void RegisterEntityType<TEntity>(string keyPrefix) where TEntity : class
        {
            if (string.IsNullOrEmpty(keyPrefix))
                throw new ArgumentException("Cache key prefix cannot be null or empty", nameof(keyPrefix));
            
            Type entityType = typeof(TEntity);
            
            lock (_entityPrefixMap)
            {
                if (!_entityPrefixMap.TryGetValue(entityType, out var prefixes))
                {
                    prefixes = new List<string>();
                    _entityPrefixMap[entityType] = prefixes;
                }
                
                if (!prefixes.Contains(keyPrefix))
                {
                    prefixes.Add(keyPrefix);
                    _logger.LogDebug("Registered entity type {EntityType} for cache invalidation with prefix {Prefix}", entityType.Name, keyPrefix);
                }
            }
        }
        
        /// <inheritdoc />
        public void RegisterEntityTypes(Dictionary<Type, string> entityTypeToPrefixMap)
        {
            if (entityTypeToPrefixMap == null)
                throw new ArgumentNullException(nameof(entityTypeToPrefixMap));
            
            foreach (var kvp in entityTypeToPrefixMap)
            {
                lock (_entityPrefixMap)
                {
                    if (!_entityPrefixMap.TryGetValue(kvp.Key, out var prefixes))
                    {
                        prefixes = new List<string>();
                        _entityPrefixMap[kvp.Key] = prefixes;
                    }
                    
                    if (!prefixes.Contains(kvp.Value))
                    {
                        prefixes.Add(kvp.Value);
                        _logger.LogDebug("Registered entity type {EntityType} for cache invalidation with prefix {Prefix}", 
                            kvp.Key.Name, kvp.Value);
                    }
                }
            }
        }
        
        /// <inheritdoc />
        public async Task ProcessChangesAsync(IEnumerable<EntityEntry> entries)
        {
            var changedEntries = entries.Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                                       .ToList();
            
            if (!changedEntries.Any())
                return;
            
            foreach (var entry in changedEntries)
            {
                await InvalidateForEntityAsync(entry.Entity.GetType(), entry.Entity, entry.State);
            }
        }
        
        /// <inheritdoc />
        public async Task InvalidateEntityCacheAsync<TEntity>() where TEntity : class
        {
            Type entityType = typeof(TEntity);
            
            if (!_entityPrefixMap.TryGetValue(entityType, out var prefixes) || prefixes.Count == 0)
            {
                _logger.LogDebug("No cache prefixes registered for entity type {EntityType}", entityType.Name);
                return;
            }
            
            foreach (var prefix in prefixes)
            {
                await _queryCache.InvalidateQueryResultsByPrefixAsync(prefix);
                _logger.LogDebug("Invalidated all caches with prefix {Prefix} for entity type {EntityType}", 
                    prefix, entityType.Name);
            }
        }
        
        /// <inheritdoc />
        public async Task InvalidateEntityCacheAsync<TEntity>(TEntity entity, EntityState action) where TEntity : class
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            
            await InvalidateForEntityAsync(typeof(TEntity), entity, action);
        }
        
        #region Private Helper Methods
        
        private async Task InvalidateForEntityAsync(Type entityType, object entity, EntityState state)
        {
            if (!_entityPrefixMap.TryGetValue(entityType, out var prefixes) || prefixes.Count == 0)
            {
                _logger.LogTrace("No cache prefixes registered for entity type {EntityType}", entityType.Name);
                return;
            }
            
            foreach (var prefix in prefixes)
            {
                await _queryCache.InvalidateQueryResultsByPrefixAsync(prefix);
                _logger.LogDebug("Invalidated cache with prefix {Prefix} due to {Action} of entity type {EntityType}", 
                    prefix, state, entityType.Name);
            }
        }
        
        #endregion
    }
}