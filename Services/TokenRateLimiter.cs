using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;

namespace WorkoutTrackerWeb.Services
{
    public interface ITokenRateLimiter
    {
        bool ShouldLimit(string clientIdentifier);
        void ResetLimit(string clientIdentifier);
        bool IsIpWhitelisted(string ipAddress);
        Task<bool> AddIpToWhitelistAsync(string ipAddress, string description = null, string createdBy = null);
        Task<bool> RemoveIpFromWhitelistAsync(string ipAddress);
        Task<IReadOnlyList<WhitelistedIp>> GetWhitelistedIpsAsync();
    }

    public class TokenRateLimiter : ITokenRateLimiter, IHostedService, IDisposable
    {
        private readonly ConcurrentDictionary<string, TokenBucket> _buckets = new();
        private readonly HashSet<string> _whitelistedIpsCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _whitelistLock = new();
        private readonly int _maxTokens;
        private readonly TimeSpan _refillTime;
        private readonly ILogger<TokenRateLimiter> _logger;
        private readonly IServiceProvider _serviceProvider;
        private Timer _cacheRefreshTimer;
        private bool _initialized = false;
        private readonly SemaphoreSlim _initLock = new SemaphoreSlim(1, 1);

        // Updated: Increased defaults to 30 tokens with 3 tokens per second refill rate
        public TokenRateLimiter(
            ILogger<TokenRateLimiter> logger,
            IServiceProvider serviceProvider,
            int maxTokens = 30,
            int refillTimeMilliseconds = 333) // Refills at ~3 tokens per second
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _maxTokens = maxTokens;
            _refillTime = TimeSpan.FromMilliseconds(refillTimeMilliseconds);
        }

        private async Task EnsureInitializedAsync()
        {
            if (_initialized)
                return;

            await _initLock.WaitAsync();
            try
            {
                if (!_initialized)
                {
                    await LoadWhitelistFromDatabaseAsync();
                    _initialized = true;
                }
            }
            finally
            {
                _initLock.Release();
            }
        }

        public bool ShouldLimit(string clientIdentifier)
        {
            // Make sure initialization is complete - this will block if necessary
            EnsureInitializedAsync().GetAwaiter().GetResult();

            if (string.IsNullOrEmpty(clientIdentifier))
            {
                // Always rate limit empty identifiers
                return true;
            }

            // Check if this is an IP address and if it's whitelisted
            if (IsIpWhitelisted(clientIdentifier))
            {
                _logger.LogDebug("Client {ClientIdentifier} is whitelisted, bypassing rate limit", clientIdentifier);
                return false; // Never limit whitelisted IPs
            }

            var bucket = _buckets.GetOrAdd(clientIdentifier, _ => new TokenBucket(_maxTokens, _refillTime));
            bool limited = !bucket.TryTake();
            
            if (limited)
            {
                _logger.LogWarning("Rate limit exceeded for client {ClientIdentifier}", clientIdentifier);
            }
            
            return limited;
        }

        public void ResetLimit(string clientIdentifier)
        {
            if (!string.IsNullOrEmpty(clientIdentifier) && _buckets.TryGetValue(clientIdentifier, out var bucket))
            {
                bucket.Reset();
                _logger.LogInformation("Rate limit reset for client {ClientIdentifier}", clientIdentifier);
            }
        }

        public bool IsIpWhitelisted(string ipAddress)
        {
            // Make sure initialization is complete - this will block if necessary
            EnsureInitializedAsync().GetAwaiter().GetResult();

            if (string.IsNullOrEmpty(ipAddress))
                return false;

            // Validate IP format
            if (!IPAddress.TryParse(ipAddress, out _))
                return false;

            lock (_whitelistLock)
            {
                return _whitelistedIpsCache.Contains(ipAddress);
            }
        }

        public async Task<bool> AddIpToWhitelistAsync(string ipAddress, string description = null, string createdBy = null)
        {
            await EnsureInitializedAsync();

            if (string.IsNullOrEmpty(ipAddress))
                return false;

            // Validate IP format
            if (!IPAddress.TryParse(ipAddress, out _))
                return false;

            // Check if already whitelisted in cache
            lock (_whitelistLock)
            {
                if (_whitelistedIpsCache.Contains(ipAddress))
                {
                    return true; // Already whitelisted
                }
            }

            // Add to database
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<WorkoutTrackerWebContext>();

                    // Check if already exists in database
                    var existingEntry = await dbContext.WhitelistedIps
                        .FirstOrDefaultAsync(w => w.IpAddress == ipAddress);

                    if (existingEntry != null)
                    {
                        // Already exists, update cache
                        lock (_whitelistLock)
                        {
                            _whitelistedIpsCache.Add(ipAddress);
                        }
                        return true;
                    }

                    // Add new entry
                    var whitelistedIp = new WhitelistedIp
                    {
                        IpAddress = ipAddress,
                        Description = description,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = createdBy
                    };

                    dbContext.WhitelistedIps.Add(whitelistedIp);
                    
                    await dbContext.SaveChangesAsync();
                    
                    // Update cache
                    lock (_whitelistLock)
                    {
                        _whitelistedIpsCache.Add(ipAddress);
                    }
                    
                    _logger.LogInformation("IP {IpAddress} added to rate limit whitelist", ipAddress);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add IP {IpAddress} to whitelist database", ipAddress);
                return false;
            }
        }

        public async Task<bool> RemoveIpFromWhitelistAsync(string ipAddress)
        {
            await EnsureInitializedAsync();

            if (string.IsNullOrEmpty(ipAddress))
                return false;

            // Remove from cache first for immediate effect
            bool removedFromCache;
            lock (_whitelistLock)
            {
                removedFromCache = _whitelistedIpsCache.Remove(ipAddress);
            }

            // Remove from database
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<WorkoutTrackerWebContext>();
                    
                    var whitelistedIp = await dbContext.WhitelistedIps
                        .FirstOrDefaultAsync(w => w.IpAddress == ipAddress);
                    
                    if (whitelistedIp == null)
                    {
                        return removedFromCache; // Not found in database, but may have been in cache
                    }
                    
                    dbContext.WhitelistedIps.Remove(whitelistedIp);
                    await dbContext.SaveChangesAsync();
                    
                    _logger.LogInformation("IP {IpAddress} removed from rate limit whitelist", ipAddress);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove IP {IpAddress} from whitelist database", ipAddress);
                
                // Revert cache removal if database operation failed
                if (removedFromCache)
                {
                    lock (_whitelistLock)
                    {
                        _whitelistedIpsCache.Add(ipAddress);
                    }
                }
                
                return false;
            }
        }

        public async Task<IReadOnlyList<WhitelistedIp>> GetWhitelistedIpsAsync()
        {
            await EnsureInitializedAsync();

            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<WorkoutTrackerWebContext>();
                    return await dbContext.WhitelistedIps.ToListAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get whitelisted IPs from database");
                throw; // Let the caller handle the error
            }
        }

        private async Task LoadWhitelistFromDatabaseAsync()
        {
            try
            {
                _logger.LogInformation("Loading IP whitelist from database");
                
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<WorkoutTrackerWebContext>();
                    
                    // Check if dbContext is accessible
                    if (dbContext.Database.CanConnect())
                    {
                        var whitelistedIps = await dbContext.WhitelistedIps.ToListAsync();
                        
                        lock (_whitelistLock)
                        {
                            _whitelistedIpsCache.Clear();
                            foreach (var ip in whitelistedIps)
                            {
                                if (!string.IsNullOrWhiteSpace(ip.IpAddress))
                                {
                                    _whitelistedIpsCache.Add(ip.IpAddress);
                                }
                            }
                        }
                        
                        _logger.LogInformation("Loaded {Count} IPs from whitelist database", whitelistedIps.Count);
                    }
                    else
                    {
                        _logger.LogWarning("Cannot connect to database to load IP whitelist");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load IP whitelist from database");
                // Don't throw - we'll start with an empty whitelist and try to reload later
            }
        }

        // IHostedService implementation to refresh the whitelist cache periodically
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Token rate limiter service started");
            
            // Initialize whitelist on startup
            try
            {
                await LoadWhitelistFromDatabaseAsync();
                _initialized = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing whitelist during startup");
            }
            
            // Set up a timer to periodically refresh the whitelist cache
            _cacheRefreshTimer = new Timer(async state => 
            {
                try
                {
                    await LoadWhitelistFromDatabaseAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error refreshing whitelist cache during timer callback");
                }
            }, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
            
            return;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Token rate limiter service stopping");
            
            // Dispose timer
            _cacheRefreshTimer?.Change(Timeout.Infinite, 0);
            
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _cacheRefreshTimer?.Dispose();
            _initLock.Dispose();
        }

        // Token bucket algorithm - tokens refill gradually over time
        private class TokenBucket
        {
            private int _tokens;
            private readonly int _capacity;
            private readonly TimeSpan _refillTime;
            private DateTime _lastRefill;
            private readonly object _lock = new();

            public TokenBucket(int capacity, TimeSpan refillTime)
            {
                _capacity = capacity;
                _tokens = capacity;
                _refillTime = refillTime;
                _lastRefill = DateTime.UtcNow;
            }

            public bool TryTake()
            {
                lock (_lock)
                {
                    RefillTokens();
                    if (_tokens > 0)
                    {
                        _tokens--;
                        return true;
                    }
                    return false;
                }
            }

            public void Reset()
            {
                lock (_lock)
                {
                    _tokens = _capacity;
                    _lastRefill = DateTime.UtcNow;
                }
            }

            private void RefillTokens()
            {
                var now = DateTime.UtcNow;
                var elapsedTime = now - _lastRefill;
                
                if (elapsedTime >= _refillTime)
                {
                    // Calculate how many tokens to add based on elapsed time
                    var periodsElapsed = (int)(elapsedTime.TotalMilliseconds / _refillTime.TotalMilliseconds);
                    var tokensToAdd = periodsElapsed;
                    
                    if (tokensToAdd > 0)
                    {
                        _tokens = Math.Min(_capacity, _tokens + tokensToAdd);
                        _lastRefill = now;
                    }
                }
            }
        }
    }
}