using Microsoft.Extensions.Logging;
using Prometheus;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace WorkoutTrackerWeb.Services.Metrics
{
    /// <summary>
    /// Service for tracking compression metrics and analytics
    /// </summary>
    public class CompressionMetricsService
    {
        private readonly ILogger<CompressionMetricsService> _logger;
        
        // Counters for tracking compression statistics
        private readonly Counter _totalBytesBeforeCompression;
        private readonly Counter _totalBytesAfterCompression;
        private readonly Counter _totalRequestsCompressed;
        private readonly Counter _compressedRequestsByType;
        private readonly Histogram _compressionRatio;

        // Running aggregates
        private long _bytesBeforeCompression = 0;
        private long _bytesAfterCompression = 0;
        private int _requestsCompressed = 0;

        public CompressionMetricsService(ILogger<CompressionMetricsService> logger)
        {
            _logger = logger;
            
            // Create Prometheus metrics
            _totalBytesBeforeCompression = Prometheus.Metrics.CreateCounter(
                "workouttracker_compression_bytes_before_total",
                "Total bytes before compression");
                
            _totalBytesAfterCompression = Prometheus.Metrics.CreateCounter(
                "workouttracker_compression_bytes_after_total",
                "Total bytes after compression");
                
            _totalRequestsCompressed = Prometheus.Metrics.CreateCounter(
                "workouttracker_compression_requests_total",
                "Total number of requests compressed");
                
            _compressedRequestsByType = Prometheus.Metrics.CreateCounter(
                "workouttracker_compression_requests_by_type_total",
                "Total number of requests compressed by content type",
                new CounterConfiguration
                {
                    LabelNames = new[] { "content_type", "compression_method" }
                });
                
            _compressionRatio = Prometheus.Metrics.CreateHistogram(
                "workouttracker_compression_ratio",
                "Compression ratio (bytes before / bytes after)",
                new HistogramConfiguration
                {
                    Buckets = new[] { 1.0, 1.5, 2.0, 2.5, 3.0, 4.0, 5.0, 10.0 }
                });
        }

        /// <summary>
        /// Record statistics for a compressed response
        /// </summary>
        public void RecordCompression(long bytesBeforeCompression, long bytesAfterCompression, 
            string contentType, string compressionMethod)
        {
            if (bytesBeforeCompression <= 0 || bytesAfterCompression <= 0)
            {
                return; // Skip invalid data
            }

            // Update Prometheus metrics
            _totalBytesBeforeCompression.Inc(bytesBeforeCompression);
            _totalBytesAfterCompression.Inc(bytesAfterCompression);
            _totalRequestsCompressed.Inc();
            _compressedRequestsByType.WithLabels(contentType, compressionMethod).Inc();
            
            // Calculate and record compression ratio
            double compressionRatio = bytesBeforeCompression / (double)bytesAfterCompression;
            _compressionRatio.Observe(compressionRatio);
            
            // Update running aggregates
            Interlocked.Add(ref _bytesBeforeCompression, bytesBeforeCompression);
            Interlocked.Add(ref _bytesAfterCompression, bytesAfterCompression);
            Interlocked.Increment(ref _requestsCompressed);
        }

        /// <summary>
        /// Gets the current bandwidth savings as a percentage
        /// </summary>
        public double GetBandwidthSavingsPercentage()
        {
            long before = Interlocked.Read(ref _bytesBeforeCompression);
            long after = Interlocked.Read(ref _bytesAfterCompression);
            
            if (before == 0 || after == 0)
                return 0;
                
            return 100 * (1 - (after / (double)before));
        }
        
        /// <summary>
        /// Gets the total bytes saved by compression
        /// </summary>
        public long GetTotalBytesSaved()
        {
            return Math.Max(0, Interlocked.Read(ref _bytesBeforeCompression) - 
                               Interlocked.Read(ref _bytesAfterCompression));
        }
        
        /// <summary>
        /// Gets the total number of requests that were compressed
        /// </summary>
        public int GetTotalRequestsCompressed()
        {
            return Interlocked.CompareExchange(ref _requestsCompressed, 0, 0);
        }
    }
}