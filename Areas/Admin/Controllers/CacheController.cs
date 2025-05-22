using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Services.Cache;
using System.Threading.Tasks;

namespace WorkoutTrackerWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CacheController : Controller
    {
        private readonly ILogger<CacheController> _logger;
        private readonly IQueryResultCacheService _queryCache;
        private readonly ICacheInvalidationService _cacheInvalidation;

        public CacheController(
            ILogger<CacheController> logger,
            IQueryResultCacheService queryCache,
            ICacheInvalidationService cacheInvalidation)
        {
            _logger = logger;
            _queryCache = queryCache;
            _cacheInvalidation = cacheInvalidation;
        }
        
        /// <summary>
        /// Display the cache dashboard showing metrics
        /// </summary>
        [HttpGet]
        public IActionResult Index()
        {
            _logger.LogInformation("Accessed cache metrics dashboard");
            return View();
        }
        
        /// <summary>
        /// Manually invalidate all cached data for a specific prefix
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InvalidatePrefix(string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                TempData["Error"] = "Cache prefix cannot be empty";
                return RedirectToAction(nameof(Index));
            }
            
            _logger.LogInformation("Manually invalidating cache prefix: {Prefix}", prefix);
            await _queryCache.InvalidateQueryResultsByPrefixAsync(prefix);
            
            TempData["Success"] = $"Successfully invalidated cache with prefix '{prefix}'";
            return RedirectToAction(nameof(Index));
        }
        
        /// <summary>
        /// Register entity types for cache invalidation
        /// </summary>
        [HttpGet]
        public IActionResult RegisterEntities()
        {
            _logger.LogInformation("Accessed entity registration for cache invalidation");
            return View();
        }
        
        /// <summary>
        /// Register common entity types for cache invalidation
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RegisterCommonEntities()
        {
            // Register common entity types with their cache prefixes
            _cacheInvalidation.RegisterEntityType<Models.WorkoutSession>("user:sessions");
            _cacheInvalidation.RegisterEntityType<Models.WorkoutExercise>("user:sessions");
            _cacheInvalidation.RegisterEntityType<Models.WorkoutSet>("user:sessions");
            
            _cacheInvalidation.RegisterEntityType<Models.WorkoutSession>("user:metrics");
            _cacheInvalidation.RegisterEntityType<Models.WorkoutExercise>("user:metrics");
            _cacheInvalidation.RegisterEntityType<Models.WorkoutSet>("user:metrics");
            
            _cacheInvalidation.RegisterEntityType<Models.WorkoutSession>("user:frequency");
            _cacheInvalidation.RegisterEntityType<Models.WorkoutSession>("user:total-workouts");
            
            _logger.LogInformation("Registered common entity types for cache invalidation");
            TempData["Success"] = "Successfully registered common entity types for cache invalidation";
            return RedirectToAction(nameof(Index));
        }
    }
}