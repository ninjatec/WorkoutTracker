using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.OutputCaching;
using WorkoutTrackerWeb.Services.Redis;

namespace WorkoutTrackerWeb.Services.Cache
{
    /// <summary>
    /// Service for invalidating cache when application version changes or container is restarted
    /// </summary>
    public class VersionCacheInvalidationService : IHostedService
    {
        private readonly ILogger<VersionCacheInvalidationService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IResilientCacheService _cacheService;
        private readonly IRedisKeyService _keyService;
        private readonly IOutputCacheStore _outputCacheStore;

        private const string VERSION_CACHE_KEY = "app:version";
        private const string DEPLOYMENT_TIMESTAMP_KEY = "app:deployment_timestamp";

        /// <summary>
        /// Instantiates a new VersionCacheInvalidationService
        /// </summary>
        public VersionCacheInvalidationService(
            ILogger<VersionCacheInvalidationService> logger,
            IServiceProvider serviceProvider,
            IResilientCacheService cacheService,
            IRedisKeyService keyService,
            IOutputCacheStore outputCacheStore)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _keyService = keyService ?? throw new ArgumentNullException(nameof(keyService));
            _outputCacheStore = outputCacheStore ?? throw new ArgumentNullException(nameof(outputCacheStore));
        }

        /// <summary>
        /// Called when the application starts
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting Version Cache Invalidation Service");
            
            try
            {
                await CheckForVersionChangeAndInvalidateCache(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while checking for version changes");
            }
        }

        /// <summary>
        /// Called when the application stops
        /// </summary>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Checks if the application version has changed and invalidates caches if needed
        /// </summary>
        private async Task CheckForVersionChangeAndInvalidateCache(CancellationToken cancellationToken)
        {
            var currentVersion = await GetCurrentAppVersionAsync();
            if (currentVersion == null)
            {
                _logger.LogWarning("Failed to read current application version, skipping cache invalidation check");
                return;
            }

            // Generate a timestamp for this deployment
            var currentTimestamp = DateTime.UtcNow.ToString("o");
            
            // Check for cached version
            var (found, cachedVersion) = await _cacheService.TryGetValueAsync<string>(VERSION_CACHE_KEY);

            // Check for cached timestamp
            var (timestampFound, cachedTimestamp) = await _cacheService.TryGetValueAsync<string>(DEPLOYMENT_TIMESTAMP_KEY);
            
            bool versionChanged = !found || cachedVersion != currentVersion;
            bool isNewDeployment = !timestampFound;

            if (versionChanged || isNewDeployment)
            {
                // Version has changed or this is a new deployment, invalidate all caches
                _logger.LogInformation(
                    versionChanged 
                        ? "Application version changed from {OldVersion} to {NewVersion}. Invalidating all caches." 
                        : "New deployment detected. Invalidating all caches.",
                    cachedVersion ?? "unknown", currentVersion);
                
                await InvalidateAllCachesAsync(cancellationToken);
                
                // Store new version and timestamp
                await _cacheService.SetAsync(VERSION_CACHE_KEY, currentVersion, TimeSpan.FromDays(30));
                await _cacheService.SetAsync(DEPLOYMENT_TIMESTAMP_KEY, currentTimestamp, TimeSpan.FromDays(30));

                _logger.LogInformation("Cache invalidation complete. New version {Version} registered with timestamp {Timestamp}",
                    currentVersion, currentTimestamp);
            }
            else
            {
                _logger.LogInformation("No version change detected. Current version: {Version}, Deployment timestamp: {Timestamp}", 
                    currentVersion, cachedTimestamp);
            }
        }

        /// <summary>
        /// Gets the current application version from version.json file
        /// </summary>
        private async Task<string> GetCurrentAppVersionAsync()
        {
            try
            {
                string versionPath = Path.Combine(AppContext.BaseDirectory, "version.json");
                if (!File.Exists(versionPath))
                {
                    _logger.LogWarning("version.json not found at: {Path}", versionPath);
                    return Guid.NewGuid().ToString(); // Use a random GUID as fallback to ensure cache invalidation
                }

                string versionJson = await File.ReadAllTextAsync(versionPath);
                var versionInfo = JsonSerializer.Deserialize<VersionInfo>(versionJson);

                return $"{versionInfo.Major}.{versionInfo.Minor}.{versionInfo.Patch}.{versionInfo.Build}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading version.json file");
                return Guid.NewGuid().ToString(); // Use a random GUID as fallback to ensure cache invalidation
            }
        }

        /// <summary>
        /// Invalidates all caches in the system
        /// </summary>
        private async Task InvalidateAllCachesAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            
            // 1. Invalidate output cache using tags
            try
            {
                _logger.LogInformation("Invalidating output cache");
                
                // Invalidate all important cache tags
                string[] commonTags = new[] { 
                    "content:home",
                    "content:workouts",
                    "content:reports",
                    "content:goals"
                };
                
                foreach (var tag in commonTags)
                {
                    await _outputCacheStore.EvictByTagAsync(tag, cancellationToken);
                    _logger.LogDebug("Invalidated output cache entries with tag {Tag}", tag);
                    
                    // Record cache metrics for the invalidation
                    try
                    {
                        CacheMetrics.RecordInvalidation($"output:{tag}", "version_change");
                    }
                    catch (Exception metricEx)
                    {
                        _logger.LogWarning(metricEx, "Failed to record cache invalidation metrics for tag {Tag}", tag);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating output cache");
            }
            
            // 2. Invalidate query cache using prefixes
            try
            {
                _logger.LogInformation("Invalidating query cache");
                var queryCache = scope.ServiceProvider.GetService<IQueryResultCacheService>();
                
                if (queryCache != null)
                {
                    // Invalidate common prefixes
                    string[] commonPrefixes = new[] {
                        "user:sessions",
                        "user:metrics",
                        "user:frequency",
                        "user:total-workouts",
                        "user:goals"
                    };
                    
                    foreach (var prefix in commonPrefixes)
                    {
                        await queryCache.InvalidateQueryResultsByPrefixAsync(prefix);
                        _logger.LogDebug("Invalidated query cache with prefix {Prefix}", prefix);
                    }
                }
                else
                {
                    _logger.LogWarning("IQueryResultCacheService not available, skipping query cache invalidation");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating query cache");
            }
            
            // 3. Clear the service worker cache by updating the version keys in service-worker.js
            // This happens automatically through the version.json change when the app is redeployed
            
            _logger.LogInformation("Cache invalidation completed successfully");
        }
        
        /// <summary>
        /// Simple class to deserialize version information
        /// </summary>
        private class VersionInfo
        {
            public int Major { get; set; }
            public int Minor { get; set; }
            public int Patch { get; set; }
            public int Build { get; set; }
        }
    }
}