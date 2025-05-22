using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkoutTrackerWeb.Models.Logging;
using WorkoutTrackerWeb.Services.Logging;

namespace WorkoutTrackerWeb.Services.Logging
{
    public class LogLevelConfigurationHostedService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LogLevelConfigurationHostedService> _logger;
        
        public LogLevelConfigurationHostedService(
            IServiceProvider serviceProvider,
            ILogger<LogLevelConfigurationHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }
        
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Log level configuration service starting");
            
            try
            {
                // Create a new scope to resolve scoped services
                using var scope = _serviceProvider.CreateScope();
                var loggingService = scope.ServiceProvider.GetRequiredService<ILoggingService>();
                
                // Apply the log settings from the database
                await loggingService.ApplyLogSettingsAsync();
                
                // Get the current settings to log them
                var settings = await loggingService.GetCurrentSettingsAsync();
                
                _logger.LogInformation("Applied log level configuration: Default={DefaultLevel}, Overrides={OverrideCount}", 
                    settings.DefaultLogLevel, 
                    settings.Overrides.Count);
                
                // Log the details of each override
                foreach (var logOverride in settings.Overrides)
                {
                    _logger.LogDebug("Log level override: {SourceContext}={Level}", 
                        logOverride.SourceContext, 
                        logOverride.LogLevel);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying log level configuration");
            }
        }
        
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Log level configuration service stopping");
            return Task.CompletedTask;
        }
    }
}