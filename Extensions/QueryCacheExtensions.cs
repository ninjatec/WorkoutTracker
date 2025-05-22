using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkoutTrackerWeb.Services.Cache;

namespace WorkoutTrackerWeb.Extensions
{
    /// <summary>
    /// Extension methods for configuring query result caching
    /// </summary>
    public static class QueryCacheExtensions
    {
        /// <summary>
        /// Adds query result caching services to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">The application configuration</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddQueryResultCaching(this IServiceCollection services, IConfiguration configuration)
        {
            // Configure cache options
            services.Configure<QueryResultCacheOptions>(configuration.GetSection("QueryResultCache"));
            
            // Register the query cache service
            services.AddScoped<IQueryResultCacheService, QueryResultCacheService>();
            
            // Register the cache invalidation service
            services.AddScoped<ICacheInvalidationService, CacheInvalidationService>();
            
            return services;
        }
    }
}