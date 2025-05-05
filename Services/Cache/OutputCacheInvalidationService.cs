using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace WorkoutTrackerWeb.Services.Cache
{
    /// <summary>
    /// Service to handle output cache invalidation based on entity changes
    /// </summary>
    public interface IOutputCacheInvalidationService
    {
        /// <summary>
        /// Register an entity type for output cache invalidation
        /// </summary>
        /// <typeparam name="TEntity">The entity type to register</typeparam>
        /// <param name="tags">The cache tags to invalidate when this entity changes</param>
        void RegisterEntityType<TEntity>(params string[] tags) where TEntity : class;
        
        /// <summary>
        /// Process database changes and invalidate related output caches
        /// </summary>
        /// <param name="entries">Changed entity entries from DbContext.ChangeTracker</param>
        /// <returns>Task representing the async operation</returns>
        Task ProcessChangesAsync(IEnumerable<EntityEntry> entries);
        
        /// <summary>
        /// Invalidate output cache for specific tags
        /// </summary>
        /// <param name="tags">The cache tags to invalidate</param>
        /// <returns>Task representing the async operation</returns>
        Task InvalidateByTagsAsync(params string[] tags);
    }
    
    /// <summary>
    /// Implementation of output cache invalidation service for database changes
    /// </summary>
    public class OutputCacheInvalidationService : IOutputCacheInvalidationService
    {
        private readonly IOutputCacheStore _outputCacheStore;
        private readonly ILogger<OutputCacheInvalidationService> _logger;
        private readonly Dictionary<Type, HashSet<string>> _entityTagsMap = new();
        
        public OutputCacheInvalidationService(
            IOutputCacheStore outputCacheStore,
            ILogger<OutputCacheInvalidationService> logger)
        {
            _outputCacheStore = outputCacheStore ?? throw new ArgumentNullException(nameof(outputCacheStore));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <inheritdoc />
        public void RegisterEntityType<TEntity>(params string[] tags) where TEntity : class
        {
            if (tags == null || tags.Length == 0)
                throw new ArgumentException("At least one tag must be provided", nameof(tags));
            
            Type entityType = typeof(TEntity);
            
            lock (_entityTagsMap)
            {
                if (!_entityTagsMap.TryGetValue(entityType, out var tagSet))
                {
                    tagSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    _entityTagsMap[entityType] = tagSet;
                }
                
                foreach (var tag in tags)
                {
                    if (tagSet.Add(tag))
                    {
                        _logger.LogDebug("Registered entity type {EntityType} for output cache invalidation with tag {Tag}", 
                            entityType.Name, tag);
                    }
                }
            }
        }
        
        /// <inheritdoc />
        public async Task ProcessChangesAsync(IEnumerable<EntityEntry> entries)
        {
            if (entries == null)
                throw new ArgumentNullException(nameof(entries));
            
            var changedEntries = entries.Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                                       .ToList();
            
            if (!changedEntries.Any())
                return;
            
            var tagsToInvalidate = new HashSet<string>();
            
            foreach (var entry in changedEntries)
            {
                var entityType = entry.Entity.GetType();
                
                // Check for the direct entity type
                if (_entityTagsMap.TryGetValue(entityType, out var tags))
                {
                    foreach (var tag in tags)
                    {
                        tagsToInvalidate.Add(tag);
                        _logger.LogTrace("Added tag {Tag} for invalidation due to {Action} of entity type {EntityType}",
                            tag, entry.State, entityType.Name);
                    }
                }
                
                // Also check for base entity types and interfaces which might be registered
                foreach (var registeredType in _entityTagsMap.Keys)
                {
                    if (registeredType != entityType && registeredType.IsAssignableFrom(entityType))
                    {
                        foreach (var tag in _entityTagsMap[registeredType])
                        {
                            tagsToInvalidate.Add(tag);
                            _logger.LogTrace("Added tag {Tag} for invalidation due to {Action} of derived entity type {EntityType} (base: {BaseType})",
                                tag, entry.State, entityType.Name, registeredType.Name);
                        }
                    }
                }
            }
            
            if (tagsToInvalidate.Count > 0)
            {
                await InvalidateByTagsAsync(tagsToInvalidate.ToArray());
            }
        }
        
        /// <inheritdoc />
        public async Task InvalidateByTagsAsync(params string[] tags)
        {
            if (tags == null || tags.Length == 0)
                throw new ArgumentException("At least one tag must be provided", nameof(tags));
            
            try
            {
                foreach (var tag in tags)
                {
                    await _outputCacheStore.EvictByTagAsync(tag, default);
                    _logger.LogDebug("Invalidated output cache entries with tag {Tag}", tag);
                    
                    // Record cache metrics for the invalidation if CacheMetrics is available
                    try
                    {
                        CacheMetrics.RecordInvalidation($"output:{tag}", "entity_change");
                    }
                    catch (Exception metricEx)
                    {
                        _logger.LogWarning(metricEx, "Failed to record cache invalidation metrics for tag {Tag}", tag);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to invalidate output cache entries for tags: {Tags}", 
                    string.Join(", ", tags));
            }
        }
    }
}