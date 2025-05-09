using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Utilities;

namespace WorkoutTrackerWeb.Middleware
{
    /// <summary>
    /// Middleware that analyzes DbContexts for shadow property conflicts during application startup in development.
    /// </summary>
    public class ShadowPropertyAnalyzerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHostEnvironment _environment;
        private readonly ILogger<ShadowPropertyAnalyzerMiddleware> _logger;
        private static bool _analyzed = false;

        public ShadowPropertyAnalyzerMiddleware(
            RequestDelegate next,
            IHostEnvironment environment,
            ILogger<ShadowPropertyAnalyzerMiddleware> logger)
        {
            _next = next;
            _environment = environment;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, WorkoutTrackerWebContext dbContext)
        {
            // Only analyze once during startup and only in development
            if (!_analyzed && _environment.IsDevelopment())
            {
                _analyzed = true;
                
                _logger.LogInformation("Analyzing database contexts for shadow property conflicts...");
                
                var analyzer = new ShadowPropertyAnalyzer(_logger);
                var conflicts = analyzer.AnalyzeContext(dbContext);
                
                int conflictCount = 0;
                foreach (var conflict in conflicts)
                {
                    conflictCount++;
                    _logger.LogWarning("Shadow property conflict: Entity {EntityType} has multiple navigations to {TargetType}: {Navigations}. " +
                                      "This can cause EF Core to create shadow properties like {ShadowProps}. " + 
                                      "Configure these relationships explicitly in OnModelCreating.",
                        conflict.EntityType,
                        conflict.TargetEntityType,
                        string.Join(", ", conflict.ConflictingNavigations),
                        string.Join(", ", conflict.ShadowProperties));
                }

                if (conflictCount > 0)
                {
                    _logger.LogWarning("{Count} shadow property conflicts detected. These should be resolved to prevent issues with migrations.", 
                        conflictCount);
                }
                else
                {
                    _logger.LogInformation("No shadow property conflicts detected.");
                }
            }

            await _next(context);
        }
    }

    /// <summary>
    /// Extension methods for adding the ShadowPropertyAnalyzerMiddleware to the application pipeline.
    /// </summary>
    public static class ShadowPropertyAnalyzerMiddlewareExtensions
    {
        /// <summary>
        /// Adds the ShadowPropertyAnalyzerMiddleware to the application pipeline.
        /// </summary>
        /// <param name="app">The application builder</param>
        /// <returns>The application builder</returns>
        public static IApplicationBuilder UseShadowPropertyAnalyzer(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ShadowPropertyAnalyzerMiddleware>();
        }
    }
}