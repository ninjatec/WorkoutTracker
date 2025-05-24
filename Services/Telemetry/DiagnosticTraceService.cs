using System;
using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace WorkoutTrackerWeb.Services.Telemetry
{
    /// <summary>
    /// Diagnostic service that creates test traces on a regular interval to verify OpenTelemetry instrumentation
    /// </summary>
    public class DiagnosticTraceService : BackgroundService
    {
        private readonly ILogger<DiagnosticTraceService> _logger;
        private readonly ActivitySource _activitySource;
        private readonly IHostEnvironment _environment;

        public DiagnosticTraceService(ILogger<DiagnosticTraceService> logger, IHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
            _activitySource = new ActivitySource("WorkoutTracker.CustomInstrumentation");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Only run in production to avoid unnecessary overhead in development
            if (_environment.IsDevelopment())
            {
                _logger.LogInformation("DiagnosticTraceService is not running in development environment");
                return;
            }

            try
            {
                // Create an initial diagnostic trace on startup
                CreateDiagnosticTrace("startup");
                
                // Create a diagnostic trace every 5 minutes to verify tracing is working
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                    CreateDiagnosticTrace("periodic");
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown, ignore
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DiagnosticTraceService");
            }
        }
        
        private void CreateDiagnosticTrace(string reason)
        {
            using (var activity = _activitySource.StartActivity($"DiagnosticTrace.{reason}", ActivityKind.Internal))
            {
                if (activity != null)
                {
                    // Add standard attributes
                    activity.SetTag("service.name", "WorkoutTracker");
                    activity.SetTag("service.instance.id", Environment.MachineName);
                    activity.SetTag("service.environment", _environment.EnvironmentName);
                    
                    // Add diagnostic attributes
                    activity.SetTag("diagnostic.reason", reason);
                    activity.SetTag("diagnostic.timestamp", DateTimeOffset.UtcNow.ToString("o"));
                    activity.SetTag("diagnostic.test", "true");
                    
                    // Create an event to make the span more visible
                    activity.AddEvent(new ActivityEvent("DiagnosticTraceCreated", 
                        DateTimeOffset.UtcNow,
                        new ActivityTagsCollection(new Dictionary<string, object>
                        {
                            ["reason"] = reason
                        })));
                    
                    _logger.LogInformation("Created diagnostic OpenTelemetry trace: {Reason}", reason);
                }
                else
                {
                    _logger.LogWarning("Failed to create diagnostic trace. ActivitySource returned null activity. " +
                                       "This may indicate a problem with the OpenTelemetry configuration.");
                }
            }
        }
    }
}
