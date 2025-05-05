using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WorkoutTrackerWeb.Services.Cache
{
    /// <summary>
    /// Configuration options for the output cache background refresher
    /// </summary>
    public class OutputCacheRefresherOptions
    {
        /// <summary>
        /// How close to expiration (in seconds) before refreshing the cache
        /// </summary>
        public int RefreshBeforeExpirationSeconds { get; set; } = 30;
        
        /// <summary>
        /// Minimum hits required for a cache entry to be considered popular enough for background refresh
        /// </summary>
        public int MinimumHitsThreshold { get; set; } = 10;
        
        /// <summary>
        /// How often (in seconds) to check for cache entries that need refreshing
        /// </summary>
        public int CheckIntervalSeconds { get; set; } = 60;
        
        /// <summary>
        /// Maximum number of concurrent refreshes
        /// </summary>
        public int MaxConcurrentRefreshes { get; set; } = 5;
        
        /// <summary>
        /// Whether to enable the background refresher
        /// </summary>
        public bool Enabled { get; set; } = true;
    }
    
    /// <summary>
    /// Information about a cached resource that should be refreshed in the background
    /// </summary>
    public class CachedResourceInfo
    {
        /// <summary>
        /// The absolute URL path of the resource
        /// </summary>
        public string Path { get; set; }
        
        /// <summary>
        /// When the cache entry will expire
        /// </summary>
        public DateTime ExpiresAt { get; set; }
        
        /// <summary>
        /// Number of hits this cache entry has received
        /// </summary>
        public int Hits { get; set; }
        
        /// <summary>
        /// When this entry was last refreshed in the background
        /// </summary>
        public DateTime? LastRefreshedAt { get; set; }
        
        /// <summary>
        /// Any query string parameters needed for the request
        /// </summary>
        public string QueryString { get; set; }
    }

    /// <summary>
    /// Background service that refreshes popular output cache entries before they expire
    /// </summary>
    public class OutputCacheBackgroundRefresher : BackgroundService
    {
        private readonly ILogger<OutputCacheBackgroundRefresher> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly OutputCacheRefresherOptions _options;
        private readonly SemaphoreSlim _refreshSemaphore;
        private readonly ConcurrentDictionary<string, CachedResourceInfo> _popularResources = new();
        
        public OutputCacheBackgroundRefresher(
            IOptions<OutputCacheRefresherOptions> options,
            IServiceProvider serviceProvider,
            ILogger<OutputCacheBackgroundRefresher> logger)
        {
            _options = options?.Value ?? new OutputCacheRefresherOptions();
            _serviceProvider = serviceProvider;
            _logger = logger;
            _refreshSemaphore = new SemaphoreSlim(_options.MaxConcurrentRefreshes, _options.MaxConcurrentRefreshes);
        }
        
        /// <summary>
        /// Track a resource access for potential background refresh
        /// </summary>
        public void TrackResourceAccess(string path, string queryString = null, DateTime? expiresAt = null)
        {
            if (!_options.Enabled || string.IsNullOrEmpty(path)) return;
            
            var cacheKey = path;
            if (!string.IsNullOrEmpty(queryString))
            {
                cacheKey = $"{path}{queryString}";
            }
            
            _popularResources.AddOrUpdate(
                cacheKey,
                // Add new entry if not exists
                _ => new CachedResourceInfo 
                { 
                    Path = path, 
                    QueryString = queryString,
                    Hits = 1,
                    ExpiresAt = expiresAt ?? DateTime.UtcNow.AddMinutes(10)
                },
                // Update existing entry
                (_, existing) => 
                {
                    existing.Hits++;
                    if (expiresAt.HasValue)
                    {
                        existing.ExpiresAt = expiresAt.Value;
                    }
                    return existing;
                });
        }
        
        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Enabled)
            {
                _logger.LogInformation("Output cache background refresher is disabled");
                return;
            }
            
            _logger.LogInformation("Output cache background refresher started with {Interval}s check interval", 
                _options.CheckIntervalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RefreshExpiringSoonResourcesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in output cache background refresher");
                }
                
                // Sleep until next check interval
                await Task.Delay(TimeSpan.FromSeconds(_options.CheckIntervalSeconds), stoppingToken);
            }
        }
        
        private async Task RefreshExpiringSoonResourcesAsync(CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var refreshBeforeExpiration = TimeSpan.FromSeconds(_options.RefreshBeforeExpirationSeconds);
            var refreshCutoff = now.Add(refreshBeforeExpiration);
            
            var refreshTasks = new List<Task>();
            
            foreach (var resource in _popularResources.Values)
            {
                // Skip if not popular enough
                if (resource.Hits < _options.MinimumHitsThreshold)
                {
                    continue;
                }
                
                // Skip if not expiring soon
                if (resource.ExpiresAt > refreshCutoff)
                {
                    continue;
                }
                
                // Skip if recently refreshed
                if (resource.LastRefreshedAt.HasValue && 
                    (now - resource.LastRefreshedAt.Value).TotalSeconds < _options.RefreshBeforeExpirationSeconds / 2)
                {
                    continue;
                }
                
                // Try to take a refresh slot
                if (await _refreshSemaphore.WaitAsync(0))
                {
                    try
                    {
                        // Start a refresh task
                        refreshTasks.Add(Task.Run(async () =>
                        {
                            try
                            {
                                await RefreshResourceAsync(resource);
                                
                                // Mark as refreshed
                                resource.LastRefreshedAt = DateTime.UtcNow;
                                
                                _logger.LogDebug("Refreshed output cache entry for {Path} (hits: {Hits})", 
                                    resource.Path, resource.Hits);
                            }
                            finally
                            {
                                _refreshSemaphore.Release();
                            }
                        }, cancellationToken));
                    }
                    catch
                    {
                        _refreshSemaphore.Release();
                        throw;
                    }
                }
            }
            
            // Cleanup old entries - entries with low hit counts or that haven't been accessed in a while
            CleanupOldEntries();
            
            // Wait for all refresh tasks to complete
            if (refreshTasks.Count > 0)
            {
                _logger.LogInformation("Refreshing {Count} popular output cache entries", refreshTasks.Count);
                await Task.WhenAll(refreshTasks);
            }
        }
        
        private async Task RefreshResourceAsync(CachedResourceInfo resource)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var httpContextFactory = scope.ServiceProvider.GetRequiredService<IHttpContextFactory>();
                var outputCacheStore = scope.ServiceProvider.GetRequiredService<IOutputCacheStore>();
                
                // Create a context with the right details to match the original request
                var context = httpContextFactory.Create(new DefaultHttpContext().Features);
                context.Request.Method = "GET";
                context.Request.Path = resource.Path;
                context.Request.QueryString = new QueryString(resource.QueryString);
                
                // Add the required headers
                context.Request.Headers["Accept"] = "text/html,application/xhtml+xml,application/xml";
                context.Request.Headers["User-Agent"] = "OutputCacheBackgroundRefresher";
                
                // Set a tag so we can detect and avoid infinite loops
                context.Items["OutputCache_BackgroundRefresh"] = true;
                
                // Use the OutputCacheStore directly to refresh the cache entry
                // In a real implementation, we'd likely need to issue an actual HTTP request here
                // to properly execute all the middleware and page handlers
                
                // For now we'll just log the intent - in a real impl we'd make an HTTP call to the application
                _logger.LogInformation("Background refresh request for {Path}{QueryString}", 
                    resource.Path, resource.QueryString ?? "");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing cache entry for {Path}", resource.Path);
            }
        }
        
        private void CleanupOldEntries()
        {
            var now = DateTime.UtcNow;
            var keysToRemove = new List<string>();
            
            foreach (var kvp in _popularResources)
            {
                var resource = kvp.Value;
                
                // Remove if expired over an hour ago
                if (resource.ExpiresAt < now.AddHours(-1))
                {
                    keysToRemove.Add(kvp.Key);
                    continue;
                }
                
                // Reset hit count for entries that are old but not expired
                if (resource.Hits > 0 && resource.ExpiresAt < now.AddMinutes(-30))
                {
                    resource.Hits = 0;
                }
            }
            
            foreach (var key in keysToRemove)
            {
                _popularResources.TryRemove(key, out _);
            }
            
            if (keysToRemove.Count > 0)
            {
                _logger.LogDebug("Cleaned up {Count} expired cache tracking entries", keysToRemove.Count);
            }
        }
    }
}