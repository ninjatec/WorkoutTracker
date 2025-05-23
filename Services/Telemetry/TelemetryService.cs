using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace WorkoutTrackerWeb.Services.Telemetry
{
    public class TelemetryService
    {
        private static readonly ActivitySource _activitySource = new("WorkoutTracker.CustomInstrumentation");
        private readonly ILogger<TelemetryService> _logger;

        public TelemetryService(ILogger<TelemetryService> logger)
        {
            _logger = logger;
        }

        public Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal)
        {
            var activity = _activitySource.StartActivity(name, kind);
            if (activity != null)
            {
                _logger.LogDebug("Started activity {ActivityName} with ID {ActivityId}", name, activity.Id);
            }
            return activity;
        }

        public static ActivitySource GetActivitySource() => _activitySource;
    }
}
