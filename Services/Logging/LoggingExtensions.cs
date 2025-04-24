using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;

namespace WorkoutTrackerWeb.Services.Logging
{
    public static class LoggingExtensions
    {
        /// <summary>
        /// Configures Serilog with dynamic log level management
        /// </summary>
        public static LoggerConfiguration ConfigureWithDynamicLevels(this LoggerConfiguration loggerConfiguration)
        {
            // Get the global level switch
            var globalSwitch = DynamicLogLevelProvider.GetGlobalLevelSwitch();
            
            // Configure Serilog with the dynamic level switch
            loggerConfiguration
                .MinimumLevel.ControlledBy(globalSwitch)
                .MinimumLevel.Override("Microsoft", DynamicLogLevelProvider.GetSourceLevelSwitch("Microsoft"))
                .MinimumLevel.Override("Microsoft.AspNetCore", DynamicLogLevelProvider.GetSourceLevelSwitch("Microsoft.AspNetCore"))
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", DynamicLogLevelProvider.GetSourceLevelSwitch("Microsoft.EntityFrameworkCore"))
                .MinimumLevel.Override("Microsoft.AspNetCore.Hosting.Diagnostics", DynamicLogLevelProvider.GetSourceLevelSwitch("Microsoft.AspNetCore.Hosting.Diagnostics"))
                .MinimumLevel.Override("System.Net.Http", DynamicLogLevelProvider.GetSourceLevelSwitch("System.Net.Http"))
                .MinimumLevel.Override("WorkoutTrackerWeb", DynamicLogLevelProvider.GetSourceLevelSwitch("WorkoutTrackerWeb"))
                .MinimumLevel.Override("WorkoutTrackerWeb.Services", DynamicLogLevelProvider.GetSourceLevelSwitch("WorkoutTrackerWeb.Services"))
                .MinimumLevel.Override("WorkoutTrackerWeb.Pages", DynamicLogLevelProvider.GetSourceLevelSwitch("WorkoutTrackerWeb.Pages"));
                
            return loggerConfiguration;
        }
        
        /// <summary>
        /// Registers logging services for dependency injection
        /// </summary>
        public static IServiceCollection AddLoggingServices(this IServiceCollection services)
        {
            // Register our logging service
            services.AddScoped<ILoggingService, LoggingService>();
            
            return services;
        }
    }
}