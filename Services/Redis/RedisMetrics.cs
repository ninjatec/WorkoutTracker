using Prometheus;

namespace WorkoutTrackerWeb.Services.Redis
{
    public static class RedisMetrics
    {
        public static readonly Gauge MemoryUsed = Prometheus.Metrics.CreateGauge(
            "redis_memory_used_bytes",
            "Redis memory usage in bytes");

        public static readonly Gauge ConnectedClients = Prometheus.Metrics.CreateGauge(
            "redis_connected_clients",
            "Number of connected Redis clients");

        public static readonly Gauge OperationsPerSecond = Prometheus.Metrics.CreateGauge(
            "redis_operations_per_second",
            "Redis operations per second");

        public static readonly Counter RedisErrors = Prometheus.Metrics.CreateCounter(
            "redis_errors_total",
            "Total number of Redis errors",
            new CounterConfiguration
            {
                LabelNames = new[] { "type" }
            });

        public static readonly Histogram RedisOperationDuration = Prometheus.Metrics.CreateHistogram(
            "redis_operation_duration_seconds",
            "Duration of Redis operations in seconds",
            new HistogramConfiguration
            {
                Buckets = Histogram.ExponentialBuckets(0.001, 2, 10),
                LabelNames = new[] { "operation" }
            });
    }
}