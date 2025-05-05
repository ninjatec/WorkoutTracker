// filepath: /Users/marccoxall/Documents/projects/WorkoutTracker/Services/Redis/NullCircuitBreakerService.cs
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace WorkoutTrackerWeb.Services.Redis
{
    /// <summary>
    /// Null implementation of IRedisCircuitBreakerService that always uses the fallback operation.
    /// Used when Redis is disabled or unavailable.
    /// </summary>
    public class NullCircuitBreakerService : IRedisCircuitBreakerService
    {
        private readonly ILogger<NullCircuitBreakerService> _logger;

        public NullCircuitBreakerService(ILogger<NullCircuitBreakerService> logger)
        {
            _logger = logger;
        }

        public CircuitState CurrentState => CircuitState.Open;
        
        public bool IsAvailable => false;

        /// <summary>
        /// Executes the fallback operation directly without attempting Redis operations
        /// </summary>
        public async Task<T> ExecuteAsync<T>(Func<IConnectionMultiplexer, Task<T>> redisOperation, Func<Task<T>> fallbackOperation)
        {
            _logger.LogDebug("Redis circuit breaker is in null state, using fallback operation directly");
            return await fallbackOperation();
        }

        /// <summary>
        /// Executes the fallback operation directly without attempting Redis operations (void version)
        /// </summary>
        public async Task ExecuteAsync(Func<IConnectionMultiplexer, Task> redisOperation, Func<Task> fallbackOperation)
        {
            _logger.LogDebug("Redis circuit breaker is in null state, using fallback operation directly");
            await fallbackOperation();
        }
    }
}