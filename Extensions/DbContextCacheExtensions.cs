using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
            // Get the cache invalidation service
            var cacheService = serviceProvider.GetService<ICacheInvalidationService>();
            if (cacheService == null)
            {
                // If cache service is not available, just save changes normally
                return context.SaveChanges();
            }

            // Capture the change tracker entries before saving
            var entries = context.ChangeTracker.Entries().ToList();
            
            // Save the changes to the database
            int result = context.SaveChanges();
            
            // Process cache invalidation asynchronously
            Task.Run(() => cacheService.ProcessChangesAsync(entries)).ConfigureAwait(false);
            
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
            // Get the cache invalidation service
            var cacheService = serviceProvider.GetService<ICacheInvalidationService>();
            if (cacheService == null)
            {
                // If cache service is not available, just save changes normally
                return await context.SaveChangesAsync(cancellationToken);
            }

            // Capture the change tracker entries before saving
            var entries = context.ChangeTracker.Entries().ToList();
            
            // Save the changes to the database
            int result = await context.SaveChangesAsync(cancellationToken);
            
            // Process cache invalidation
            await cacheService.ProcessChangesAsync(entries);
            
            return result;
        }
    }
}