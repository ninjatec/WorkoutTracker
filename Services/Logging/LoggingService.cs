using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models.Logging;

namespace WorkoutTrackerWeb.Services.Logging
{
    public class LoggingService : ILoggingService
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly ILogger<LoggingService> _logger;
        private readonly LoggingLevelSwitch _levelSwitch;
        private readonly Dictionary<string, LoggingLevelSwitch> _sourceContextSwitches = new();

        private static readonly string[] _commonSourceContexts = new[]
        {
            "Microsoft",
            "Microsoft.AspNetCore",
            "Microsoft.EntityFrameworkCore",
            "Microsoft.AspNetCore.Hosting.Diagnostics",
            "WorkoutTrackerWeb",
            "WorkoutTrackerWeb.Services",
            "WorkoutTrackerWeb.Pages",
            "System.Net.Http",
            "Hangfire",
            "Hangfire.Server",
            "Hangfire.Client", 
            "Hangfire.SqlServer",
            "Hangfire.Storage",
            "WorkoutTrackerWeb.Services.Hangfire"
        };

        public LoggingService(
            WorkoutTrackerWebContext context,
            ILogger<LoggingService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Get the global level switch from Serilog
            _levelSwitch = GetGlobalLevelSwitch();
            
            // Initialize source context switches from Serilog configuration
            foreach (var sourceContext in _commonSourceContexts)
            {
                _sourceContextSwitches[sourceContext] = GetOrCreateSourceLevelSwitch(sourceContext);
            }
        }

        public async Task<LogEventLevel> GetDefaultLogLevelAsync()
        {
            var settings = await GetOrCreateSettingsAsync();
            return settings.DefaultLogLevel;
        }

        public async Task SetDefaultLogLevelAsync(LogEventLevel level, string updatedBy)
        {
            var settings = await GetOrCreateSettingsAsync();
            
            // Update the settings
            settings.DefaultLogLevel = level;
            settings.LastUpdated = DateTime.UtcNow;
            settings.LastUpdatedBy = updatedBy;
            
            // Save to database
            await _context.SaveChangesAsync();
            
            // Apply the changes to the logger
            _levelSwitch.MinimumLevel = level;
            
            _logger.LogInformation("Default log level changed to {LogLevel} by {User}", 
                level, updatedBy);
        }

        public async Task<IEnumerable<LogLevelOverride>> GetLogLevelOverridesAsync()
        {
            var settings = await GetOrCreateSettingsAsync();
            return settings.Overrides;
        }

        public async Task<LogLevelOverride> GetLogLevelOverrideAsync(string sourceContext)
        {
            var settings = await GetOrCreateSettingsAsync();
            return settings.Overrides.FirstOrDefault(o => o.SourceContext == sourceContext);
        }

        public async Task SetLogLevelOverrideAsync(string sourceContext, LogEventLevel level, string updatedBy)
        {
            var settings = await GetOrCreateSettingsAsync();
            var existing = settings.Overrides.FirstOrDefault(o => o.SourceContext == sourceContext);
            
            if (existing != null)
            {
                // Update existing override
                existing.LogLevel = level;
            }
            else
            {
                // Create new override
                settings.Overrides.Add(new LogLevelOverride
                {
                    SourceContext = sourceContext,
                    LogLevel = level,
                    LogLevelSettingsId = settings.Id
                });
            }
            
            // Update last modified
            settings.LastUpdated = DateTime.UtcNow;
            settings.LastUpdatedBy = updatedBy;
            
            // Save to database
            await _context.SaveChangesAsync();
            
            // Apply the changes to the logger
            var levelSwitch = GetOrCreateSourceLevelSwitch(sourceContext);
            levelSwitch.MinimumLevel = level;
            
            _logger.LogInformation("Log level for {SourceContext} changed to {LogLevel} by {User}", 
                sourceContext, level, updatedBy);
        }

        public async Task RemoveLogLevelOverrideAsync(string sourceContext, string updatedBy)
        {
            var settings = await GetOrCreateSettingsAsync();
            var existing = settings.Overrides.FirstOrDefault(o => o.SourceContext == sourceContext);
            
            if (existing != null)
            {
                // Remove the override
                settings.Overrides.Remove(existing);
                _context.Remove(existing);
                
                // Update last modified
                settings.LastUpdated = DateTime.UtcNow;
                settings.LastUpdatedBy = updatedBy;
                
                // Save to database
                await _context.SaveChangesAsync();
                
                // Apply the changes to the logger - revert to default
                var levelSwitch = GetOrCreateSourceLevelSwitch(sourceContext);
                levelSwitch.MinimumLevel = settings.DefaultLogLevel;
                
                _logger.LogInformation("Log level override for {SourceContext} removed by {User}", 
                    sourceContext, updatedBy);
            }
        }

        public async Task<LogLevelSettings> GetCurrentSettingsAsync()
        {
            return await GetOrCreateSettingsAsync();
        }

        public async Task ApplyLogSettingsAsync()
        {
            var settings = await GetOrCreateSettingsAsync();
            
            // Apply default log level
            _levelSwitch.MinimumLevel = settings.DefaultLogLevel;
            
            // Apply overrides
            foreach (var overrideItem in settings.Overrides)
            {
                var levelSwitch = GetOrCreateSourceLevelSwitch(overrideItem.SourceContext);
                levelSwitch.MinimumLevel = overrideItem.LogLevel;
            }
            
            _logger.LogInformation("Applied log settings: default level {DefaultLevel} with {OverrideCount} overrides", 
                settings.DefaultLogLevel, settings.Overrides.Count);
        }

        public IEnumerable<string> GetCommonSourceContexts()
        {
            return _commonSourceContexts;
        }

        public IEnumerable<KeyValuePair<string, int>> GetLogLevelOptions()
        {
            return new Dictionary<string, int>
            {
                { "Verbose", (int)LogEventLevel.Verbose },
                { "Debug", (int)LogEventLevel.Debug },
                { "Information", (int)LogEventLevel.Information },
                { "Warning", (int)LogEventLevel.Warning },
                { "Error", (int)LogEventLevel.Error },
                { "Fatal", (int)LogEventLevel.Fatal }
            };
        }

        #region Helper Methods

        private async Task<LogLevelSettings> GetOrCreateSettingsAsync()
        {
            // Get the current settings or create default ones
            var settings = await _context.Set<LogLevelSettings>()
                .Include(s => s.Overrides)
                .FirstOrDefaultAsync();
                
            if (settings == null)
            {
                // Create default settings
                settings = new LogLevelSettings
                {
                    DefaultLogLevel = LogEventLevel.Information,
                    LastUpdated = DateTime.UtcNow,
                    LastUpdatedBy = "System",
                    Overrides = new List<LogLevelOverride>
                    {
                        new LogLevelOverride
                        {
                            SourceContext = "Microsoft",
                            LogLevel = LogEventLevel.Information
                        },
                        new LogLevelOverride
                        {
                            SourceContext = "Microsoft.AspNetCore",
                            LogLevel = LogEventLevel.Warning
                        },
                        new LogLevelOverride
                        {
                            SourceContext = "Microsoft.EntityFrameworkCore",
                            LogLevel = LogEventLevel.Warning
                        },
                        new LogLevelOverride
                        {
                            SourceContext = "Hangfire",
                            LogLevel = LogEventLevel.Information
                        },
                        new LogLevelOverride
                        {
                            SourceContext = "Hangfire.Server",
                            LogLevel = LogEventLevel.Warning
                        }
                    }
                };
                
                _context.Add(settings);
                await _context.SaveChangesAsync();
            }
            
            return settings;
        }

        private LoggingLevelSwitch GetGlobalLevelSwitch()
        {
            // Access the global level switch from Serilog
            var levelSwitch = new LoggingLevelSwitch();
            
            // Try to get the current level from the logger configuration
            try
            {
                // Get the current level
                var currentLevel = Log.IsEnabled(LogEventLevel.Verbose) ? LogEventLevel.Verbose :
                    Log.IsEnabled(LogEventLevel.Debug) ? LogEventLevel.Debug :
                    Log.IsEnabled(LogEventLevel.Information) ? LogEventLevel.Information :
                    Log.IsEnabled(LogEventLevel.Warning) ? LogEventLevel.Warning :
                    Log.IsEnabled(LogEventLevel.Error) ? LogEventLevel.Error :
                    LogEventLevel.Fatal;
                
                levelSwitch.MinimumLevel = currentLevel;
            }
            catch (Exception ex)
            {
                // If something goes wrong, default to Information
                _logger.LogWarning(ex, "Error getting current log level, defaulting to Information");
                levelSwitch.MinimumLevel = LogEventLevel.Information;
            }
            
            return levelSwitch;
        }

        private LoggingLevelSwitch GetOrCreateSourceLevelSwitch(string sourceContext)
        {
            if (_sourceContextSwitches.TryGetValue(sourceContext, out var existingSwitch))
            {
                return existingSwitch;
            }
            
            var newSwitch = new LoggingLevelSwitch(_levelSwitch.MinimumLevel);
            _sourceContextSwitches[sourceContext] = newSwitch;
            return newSwitch;
        }

        #endregion
    }
}