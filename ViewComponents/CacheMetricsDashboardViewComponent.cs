using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkoutTrackerWeb.Services.Cache;

namespace WorkoutTrackerWeb.ViewComponents
{
    public class CacheMetricsDashboardViewModel
    {
        public bool IsEnabled { get; set; }
        public Dictionary<string, CachePrefixMetrics> Metrics { get; set; } = new Dictionary<string, CachePrefixMetrics>();
        public TimeSpan DefaultExpiration { get; set; }
        public bool DefaultSlidingExpiration { get; set; }
        public double OverallHitRate { get; set; }
        public double TotalHits { get; set; }
        public double TotalMisses { get; set; }
        public int TotalSize { get; set; }
    }

    public class CacheMetricsDashboardViewComponent : ViewComponent
    {
        private readonly QueryResultCacheOptions _cacheOptions;

        public CacheMetricsDashboardViewComponent(IOptions<QueryResultCacheOptions> cacheOptions)
        {
            _cacheOptions = cacheOptions.Value;
        }

        public IViewComponentResult Invoke()
        {
            var metrics = CacheMetrics.GetAllMetrics();
            
            double totalHits = 0;
            double totalMisses = 0;
            int totalSize = 0;
            
            // Calculate overall statistics
            foreach (var metric in metrics.Values)
            {
                totalHits += metric.Hits;
                totalMisses += metric.Misses;
                totalSize += metric.CurrentSize;
            }
            
            double overallHitRate = (totalHits + totalMisses > 0) 
                ? (totalHits / (totalHits + totalMisses)) * 100 
                : 0;
            
            var viewModel = new CacheMetricsDashboardViewModel
            {
                IsEnabled = _cacheOptions.Enabled,
                Metrics = metrics.OrderByDescending(m => m.Value.Hits + m.Value.Misses)
                                .ToDictionary(k => k.Key, v => v.Value),
                DefaultExpiration = _cacheOptions.DefaultExpiration,
                DefaultSlidingExpiration = _cacheOptions.DefaultSlidingExpiration,
                OverallHitRate = overallHitRate,
                TotalHits = totalHits,
                TotalMisses = totalMisses,
                TotalSize = totalSize
            };
            
            return View(viewModel);
        }
    }
}