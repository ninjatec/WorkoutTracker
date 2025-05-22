using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog.Events;
using WorkoutTrackerWeb.Models.Logging;

namespace WorkoutTrackerWeb.Services.Logging
{
    public interface ILoggingService
    {
        /// <summary>
        /// Gets the current default log level setting
        /// </summary>
        Task<LogEventLevel> GetDefaultLogLevelAsync();
        
        /// <summary>
        /// Sets the default log level and persists the change
        /// </summary>
        Task SetDefaultLogLevelAsync(LogEventLevel level, string updatedBy);
        
        /// <summary>
        /// Gets all current log level overrides
        /// </summary>
        Task<IEnumerable<LogLevelOverride>> GetLogLevelOverridesAsync();
        
        /// <summary>
        /// Gets a specific log level override by source context
        /// </summary>
        Task<LogLevelOverride> GetLogLevelOverrideAsync(string sourceContext);
        
        /// <summary>
        /// Adds or updates a log level override for a specific source context
        /// </summary>
        Task SetLogLevelOverrideAsync(string sourceContext, LogEventLevel level, string updatedBy);
        
        /// <summary>
        /// Removes a log level override for a specific source context
        /// </summary>
        Task RemoveLogLevelOverrideAsync(string sourceContext, string updatedBy);
        
        /// <summary>
        /// Gets the current log level settings including all overrides
        /// </summary>
        Task<LogLevelSettings> GetCurrentSettingsAsync();
        
        /// <summary>
        /// Applies the current log settings to the Serilog logger
        /// </summary>
        Task ApplyLogSettingsAsync();
        
        /// <summary>
        /// Gets a list of common source contexts that can be configured
        /// </summary>
        IEnumerable<string> GetCommonSourceContexts();
        
        /// <summary>
        /// Gets all possible log level options as name-value pairs
        /// </summary>
        IEnumerable<KeyValuePair<string, int>> GetLogLevelOptions();
    }
}