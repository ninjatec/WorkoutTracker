using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;

namespace WorkoutTrackerWeb.Services.Logging
{
    public class DynamicLogLevelProvider
    {
        // Static collection of level switches for global access
        private static readonly LoggingLevelSwitch GlobalLevelSwitch = new(LogEventLevel.Information);
        
        // Method to get the global level switch
        public static LoggingLevelSwitch GetGlobalLevelSwitch() => GlobalLevelSwitch;
        
        // Dictionary of source-specific level switches
        private static readonly Dictionary<string, LoggingLevelSwitch> SourceLevelSwitches = new();
        
        // Method to get a source-specific level switch
        public static LoggingLevelSwitch GetSourceLevelSwitch(string sourceContext)
        {
            if (SourceLevelSwitches.TryGetValue(sourceContext, out var existingSwitch))
            {
                return existingSwitch;
            }
            
            var newSwitch = new LoggingLevelSwitch(GlobalLevelSwitch.MinimumLevel);
            SourceLevelSwitches[sourceContext] = newSwitch;
            return newSwitch;
        }
        
        // Method to reload all switches from settings
        public static void ReloadSwitches(LogEventLevel defaultLevel, Dictionary<string, LogEventLevel> overrides)
        {
            // Update the global level switch
            GlobalLevelSwitch.MinimumLevel = defaultLevel;
            
            // Update source-specific switches
            foreach (var key in SourceLevelSwitches.Keys)
            {
                if (overrides.TryGetValue(key, out var level))
                {
                    SourceLevelSwitches[key].MinimumLevel = level;
                }
                else
                {
                    // Reset to default if no override exists
                    SourceLevelSwitches[key].MinimumLevel = defaultLevel;
                }
            }
            
            // Add any new switches from overrides
            foreach (var logOverride in overrides)
            {
                if (!SourceLevelSwitches.ContainsKey(logOverride.Key))
                {
                    SourceLevelSwitches[logOverride.Key] = new LoggingLevelSwitch(logOverride.Value);
                }
            }
        }
    }
}