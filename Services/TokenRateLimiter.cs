using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace WorkoutTrackerWeb.Services
{
    public interface ITokenRateLimiter
    {
        bool ShouldLimit(string clientIdentifier);
        void ResetLimit(string clientIdentifier);
    }

    public class TokenRateLimiter : ITokenRateLimiter
    {
        private readonly ConcurrentDictionary<string, TokenBucket> _buckets = new();
        private readonly int _maxTokens;
        private readonly TimeSpan _refillTime;
        private readonly ILogger<TokenRateLimiter> _logger;

        // Default: 10 attempts per second with a bucket of 10 tokens
        public TokenRateLimiter(
            ILogger<TokenRateLimiter> logger,
            int maxTokens = 10,
            int refillTimeSeconds = 1)  // Changed from 60 to 1 second for 10 req/sec
        {
            _logger = logger;
            _maxTokens = maxTokens;
            _refillTime = TimeSpan.FromSeconds(refillTimeSeconds);
        }

        public bool ShouldLimit(string clientIdentifier)
        {
            if (string.IsNullOrEmpty(clientIdentifier))
            {
                // Always rate limit empty identifiers
                return true;
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