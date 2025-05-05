using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Prometheus;

namespace WorkoutTrackerWeb.Services.Cache
{
    /// <summary>
    /// Service for tracking and exposing cache performance metrics
    /// </summary>
    public static class CacheMetrics
    {
        // Counters for cache operations
        private static readonly Counter _cacheHits = Prometheus.Metrics.CreateCounter(
            "query_cache_hits_total", 
            "Total number of query cache hits",
            new CounterConfiguration { LabelNames = new[] { "prefix" } });
        
        private static readonly Counter _cacheMisses = Prometheus.Metrics.CreateCounter(
            "query_cache_misses_total", 
            "Total number of query cache misses",
            new CounterConfiguration { LabelNames = new[] { "prefix" } });
        
        private static readonly Counter _cacheInvalidations = Prometheus.Metrics.CreateCounter(
            "query_cache_invalidations_total", 
            "Total number of query cache invalidations",
            new CounterConfiguration { LabelNames = new[] { "prefix", "reason" } });
        
        private static readonly Histogram _cacheOperationDuration = Prometheus.Metrics.CreateHistogram(
            "query_cache_operation_duration_seconds",
            "Duration of query cache operations in seconds",
            new HistogramConfiguration
            {
                LabelNames = new[] { "operation" },
                Buckets = new[] { 0.001, 0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10 }
            });
        
        private static readonly Gauge _cacheSize = Prometheus.Metrics.CreateGauge(
            "query_cache_size_entries",
            "Number of entries in the query cache",
            new GaugeConfiguration { LabelNames = new[] { "prefix" } });
        
        // Dictionary to keep track of current cache size by prefix
        private static readonly ConcurrentDictionary<string, int> _currentSizes = new();
        
        /// <summary>
        /// Record a cache hit for the specified prefix
        /// </summary>
        /// <param name="prefix">The cache key prefix</param>
        public static void RecordHit(string prefix)
        {
            string normalizedPrefix = NormalizePrefix(prefix);
            _cacheHits.WithLabels(normalizedPrefix).Inc();
        }
        
        /// <summary>
        /// Record a cache miss for the specified prefix
        /// </summary>
        /// <param name="prefix">The cache key prefix</param>
        public static void RecordMiss(string prefix)
        {
            string normalizedPrefix = NormalizePrefix(prefix);
            _cacheMisses.WithLabels(normalizedPrefix).Inc();
        }
        
        /// <summary>
        /// Record a cache invalidation event
        /// </summary>
        /// <param name="prefix">The cache key prefix</param>
        /// <param name="reason">The reason for invalidation (e.g. "entity_change", "manual", "ttl")</param>
        public static void RecordInvalidation(string prefix, string reason)
        {
            string normalizedPrefix = NormalizePrefix(prefix);
            _cacheInvalidations.WithLabels(normalizedPrefix, reason).Inc();
            
            // Reset size counter for this prefix
            if (_currentSizes.TryGetValue(normalizedPrefix, out _))
            {
                _currentSizes[normalizedPrefix] = 0;
                _cacheSize.WithLabels(normalizedPrefix).Set(0);
            }
        }
        
        /// <summary>
        /// Record the duration of a cache operation
        /// </summary>
        /// <param name="operation">Name of the operation (e.g. "get", "set", "remove")</param>
        /// <param name="durationSeconds">Duration in seconds</param>
        public static void RecordOperationDuration(string operation, double durationSeconds)
        {
            _cacheOperationDuration.WithLabels(operation).Observe(durationSeconds);
        }
        
        /// <summary>
        /// Returns a timer that will record the duration of a cache operation when disposed
        /// </summary>
        /// <param name="operation">Name of the operation (e.g. "get", "set", "remove")</param>
        /// <returns>An IDisposable timer that records duration on dispose</returns>
        public static IDisposable TimeOperation(string operation)
        {
            return _cacheOperationDuration.WithLabels(operation).NewTimer();
        }
        
        /// <summary>
        /// Increment the size counter for a cache prefix
        /// </summary>
        /// <param name="prefix">The cache key prefix</param>
        public static void IncrementSize(string prefix)
        {
            string normalizedPrefix = NormalizePrefix(prefix);
            int newSize = _currentSizes.AddOrUpdate(
                normalizedPrefix,
                1,
                (_, current) => current + 1);
            
            _cacheSize.WithLabels(normalizedPrefix).Set(newSize);
        }
        
        /// <summary>
        /// Decrement the size counter for a cache prefix
        /// </summary>
        /// <param name="prefix">The cache key prefix</param>
        public static void DecrementSize(string prefix)
        {
            string normalizedPrefix = NormalizePrefix(prefix);
            if (_currentSizes.TryGetValue(normalizedPrefix, out _))
            {
                int newSize = _currentSizes.AddOrUpdate(
                    normalizedPrefix,
                    0,
                    (_, current) => Math.Max(0, current - 1));
                
                _cacheSize.WithLabels(normalizedPrefix).Set(newSize);
            }
        }
        
        /// <summary>
        /// Get current hit rate for a specific prefix
        /// </summary>
        /// <param name="prefix">The cache key prefix</param>
        /// <returns>Hit rate percentage (0-100)</returns>
        public static double GetHitRate(string prefix)
        {
            string normalizedPrefix = NormalizePrefix(prefix);
            
            double hits = _cacheHits.WithLabels(normalizedPrefix).Value;
            double misses = _cacheMisses.WithLabels(normalizedPrefix).Value;
            double total = hits + misses;
            
            if (total == 0)
                return 0;
                
            return (hits / total) * 100;
        }
        
        /// <summary>
        /// Get all metrics as a dictionary
        /// </summary>
        /// <returns>Dictionary of metrics by prefix</returns>
        public static Dictionary<string, CachePrefixMetrics> GetAllMetrics()
        {
            var result = new Dictionary<string, CachePrefixMetrics>();
            
            // Collect all prefixes from all counters
            var allPrefixes = new HashSet<string>();
            foreach (var key in _currentSizes.Keys)
            {
                allPrefixes.Add(key);
            }
            
            // Create metrics entries for each prefix
            foreach (var prefix in allPrefixes)
            {
                double hits = _cacheHits.WithLabels(prefix).Value;
                double misses = _cacheMisses.WithLabels(prefix).Value;
                double invalidations = 0;
                
                // Sum up invalidations for all reasons
                foreach (var reason in new[] { "entity_change", "manual", "ttl" })
                {
                    invalidations += _cacheInvalidations.WithLabels(prefix, reason).Value;
                }
                
                int size = 0;
                _currentSizes.TryGetValue(prefix, out size);
                
                double hitRate = (hits + misses > 0) ? (hits / (hits + misses)) * 100 : 0;
                
                result[prefix] = new CachePrefixMetrics
                {
                    Hits = hits,
                    Misses = misses,
                    Invalidations = invalidations,
                    CurrentSize = size,
                    HitRate = hitRate
                };
            }
            
            return result;
        }
        
        private static string NormalizePrefix(string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
                return "default";
                
            // Remove trailing colon if present
            if (prefix.EndsWith(":"))
                return prefix.Substring(0, prefix.Length - 1);
                
            return prefix;
        }
    }

    /// <summary>
    /// Data structure for cache metrics by prefix
    /// </summary>
    public class CachePrefixMetrics
    {
        /// <summary>
        /// Number of cache hits
        /// </summary>
        public double Hits { get; set; }
        
        /// <summary>
        /// Number of cache misses
        /// </summary>
        public double Misses { get; set; }
        
        /// <summary>
        /// Number of cache invalidations
        /// </summary>
        public double Invalidations { get; set; }
        
        /// <summary>
        /// Current number of entries
        /// </summary>
        public int CurrentSize { get; set; }
        
        /// <summary>
        /// Hit rate percentage (0-100)
        /// </summary>
        public double HitRate { get; set; }
    }
}