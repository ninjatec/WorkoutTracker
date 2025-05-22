using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using WorkoutTrackerWeb.Services.Cache;

namespace WorkoutTrackerWeb.Extensions
{
    /// <summary>
    /// Extension methods for DbContext to enable cache invalidation
    /// </summary>
    public static class DbContextCacheExtensions
    {
        /// <summary>
        /// Saves all changes with automatic cache invalidation
        /// </summary>
        /// <param name="context">The DbContext instance</param>
        /// <param name="serviceProvider">The service provider to resolve cache invalidation service</param>
        /// <returns>The number of state entries written to the database</returns>
        public static int SaveChangesWithCacheInvalidation(this DbContext context, IServiceProvider serviceProvider)
        {
            // Capture the change tracker entries before saving
            var entries = context.ChangeTracker.Entries().ToList();
            
            // Save the changes to the database
            int result = context.SaveChanges();
            
            // Process cache invalidation asynchronously
            Task.Run(() => ProcessCacheInvalidationAsync(serviceProvider, entries)).ConfigureAwait(false);
            
            return result;
        }
        
        /// <summary>
        /// Saves all changes with automatic cache invalidation asynchronously
        /// </summary>
        /// <param name="context">The DbContext instance</param>
        /// <param name="serviceProvider">The service provider to resolve cache invalidation service</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>A task representing the asynchronous operation, with the number of state entries written to the database</returns>
        public static async Task<int> SaveChangesWithCacheInvalidationAsync(this DbContext context, 
            IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
        {
            // Capture the change tracker entries before saving
            var entries = context.ChangeTracker.Entries().ToList();
            
            // Save the changes to the database
            int result = await context.SaveChangesAsync(cancellationToken);
            
            // Process cache invalidation
            await ProcessCacheInvalidationAsync(serviceProvider, entries);
            
            return result;
        }

        /// <summary>
        /// Process cache invalidation for both query cache and output cache
        /// </summary>
        private static async Task ProcessCacheInvalidationAsync(IServiceProvider serviceProvider, IEnumerable<EntityEntry> entries)
        {
            var tasks = new List<Task>();

            // Get and use the query cache invalidation service if available
            var queryCacheService = serviceProvider.GetService<ICacheInvalidationService>();
            if (queryCacheService != null)
            {
                tasks.Add(queryCacheService.ProcessChangesAsync(entries));
            }

            // Get and use the output cache invalidation service if available
            var outputCacheService = serviceProvider.GetService<IOutputCacheInvalidationService>();
            if (outputCacheService != null)
            {
                tasks.Add(outputCacheService.ProcessChangesAsync(entries));
            }

            // Wait for all invalidation tasks to complete
            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
            }
        }
    }
}