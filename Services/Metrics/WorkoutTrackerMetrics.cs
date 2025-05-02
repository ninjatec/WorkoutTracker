using Prometheus;

namespace WorkoutTrackerWeb.Services.Metrics
{
    public static class WorkoutTrackerMetrics
    {
        public static readonly Counter SessionsCreated = Prometheus.Metrics.CreateCounter(
            "workout_tracker_sessions_created_total", "Number of workout sessions created");
        
        public static readonly Counter SetsCreated = Prometheus.Metrics.CreateCounter(
            "workout_tracker_sets_created_total", "Number of exercise sets created");
            
        public static readonly Counter RepsCreated = Prometheus.Metrics.CreateCounter(
            "workout_tracker_reps_created_total", "Number of exercise reps created");

        public static readonly Gauge ActiveUsers = Prometheus.Metrics.CreateGauge(
            "workout_tracker_active_users", "Number of currently active users");
            
        public static readonly Histogram HttpRequestDuration = Prometheus.Metrics.CreateHistogram(
            "workout_tracker_http_request_duration_seconds", 
            "Duration of HTTP requests in seconds",
            new HistogramConfiguration
            {
                Buckets = Histogram.ExponentialBuckets(0.01, 2, 10)
            });
    }
}