// filepath: /Users/marccoxall/Documents/projects/WorkoutTracker/Services/Redis/RedisCircuitBreakerService.cs
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace WorkoutTrackerWeb.Services.Redis
{
    /// <summary>
    /// Circuit breaker states for Redis operations
    /// </summary>
    public enum CircuitState
    {
        Closed,      // Normal operation, requests pass through
        Open,        // Circuit is open, requests fail fast
        HalfOpen     // Testing if service has recovered
    }

    /// <summary>
    /// Configuration options for the Redis circuit breaker
    /// </summary>
    public class CircuitBreakerOptions
    {
        /// <summary>
        /// Number of consecutive failures before opening the circuit
        /// </summary>
        public int FailureThreshold { get; set; } = 5;

        /// <summary>
        /// Time in seconds to keep circuit open before trying half-open state
        /// </summary>
        public int ResetTimeoutSeconds { get; set; } = 60;

        /// <summary>
        /// Maximum backoff time in seconds for retry
        /// </summary>
        public int MaxBackoffSeconds { get; set; } = 30;

        /// <summary>
        /// Base for exponential backoff calculation
        /// </summary>
        public double BackoffExponent { get; set; } = 1.5;

        /// <summary>
        /// Initial backoff time in milliseconds
        /// </summary>
        public int InitialBackoffMs { get; set; } = 100;
    }

    public interface IRedisCircuitBreakerService
    {
        Task<T> ExecuteAsync<T>(Func<IConnectionMultiplexer, Task<T>> redisOperation, Func<Task<T>> fallbackOperation);
        Task ExecuteAsync(Func<IConnectionMultiplexer, Task> redisOperation, Func<Task> fallbackOperation);
        bool IsAvailable { get; }
        CircuitState CurrentState { get; }
    }

    /// <summary>
    /// Service that provides circuit breaker functionality for Redis operations,
    /// with exponential backoff and fallback mechanisms
    /// </summary>
    public class RedisCircuitBreakerService : IRedisCircuitBreakerService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<RedisCircuitBreakerService> _logger;
        private readonly CircuitBreakerOptions _options;

        private int _failureCount;
        private DateTime _lastFailureTime = DateTime.MinValue;
        private DateTime _circuitOpenTime = DateTime.MinValue;
        private CircuitState _state = CircuitState.Closed;
        private readonly SemaphoreSlim _stateChangeLock = new SemaphoreSlim(1, 1);

        public CircuitState CurrentState => _state;
        public bool IsAvailable => _state != CircuitState.Open;

        public RedisCircuitBreakerService(
            IConnectionMultiplexer redis,
            IOptions<CircuitBreakerOptions> options,
            ILogger<RedisCircuitBreakerService> logger)
        {
            _redis = redis ?? throw new ArgumentNullException(nameof(redis));
            _options = options?.Value ?? new CircuitBreakerOptions();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Execute a Redis operation with circuit breaker protection and fallback
        /// </summary>
        /// <typeparam name="T">Return type of the operation</typeparam>
        /// <param name="redisOperation">The Redis operation to execute</param>
        /// <param name="fallbackOperation">Fallback operation if Redis is unavailable</param>
        /// <returns>Result of either the Redis operation or fallback</returns>
        public async Task<T> ExecuteAsync<T>(Func<IConnectionMultiplexer, Task<T>> redisOperation, Func<Task<T>> fallbackOperation)
        {
            if (_state == CircuitState.Open)
            {
                // Check if it's time to transition to half-open
                if (DateTime.UtcNow >= _circuitOpenTime.AddSeconds(_options.ResetTimeoutSeconds))
                {
                    await TryTransitionToHalfOpenAsync();
                }
                else
                {
                    _logger.LogDebug("Circuit open, using fallback operation");
                    return await fallbackOperation();
                }
            }

            try
            {
                if (_state == CircuitState.HalfOpen)
                {
                    _logger.LogDebug("Testing circuit in half-open state");
                }

                // Execute the Redis operation
                var result = await redisOperation(_redis);
                
                // Success - reset failures and close the circuit if needed
                await ResetCircuitAsync();
                
                return result;
            }
            catch (RedisConnectionException ex)
            {
                return await HandleFailureAsync(ex, fallbackOperation);
            }
            catch (RedisTimeoutException ex)
            {
                return await HandleFailureAsync(ex, fallbackOperation);
            }
            catch (Exception ex)
            {
                // Only increment failure count for connection-related exceptions
                if (IsConnectionFailure(ex))
                {
                    return await HandleFailureAsync(ex, fallbackOperation);
                }
                throw; // Rethrow other exceptions
            }
        }

        /// <summary>
        /// Execute a Redis operation with circuit breaker protection and fallback (void version)
        /// </summary>
        /// <param name="redisOperation">The Redis operation to execute</param>
        /// <param name="fallbackOperation">Fallback operation if Redis is unavailable</param>
        public async Task ExecuteAsync(Func<IConnectionMultiplexer, Task> redisOperation, Func<Task> fallbackOperation)
        {
            if (_state == CircuitState.Open)
            {
                // Check if it's time to transition to half-open
                if (DateTime.UtcNow >= _circuitOpenTime.AddSeconds(_options.ResetTimeoutSeconds))
                {
                    await TryTransitionToHalfOpenAsync();
                }
                else
                {
                    _logger.LogDebug("Circuit open, using fallback operation");
                    await fallbackOperation();
                    return;
                }
            }

            try
            {
                if (_state == CircuitState.HalfOpen)
                {
                    _logger.LogDebug("Testing circuit in half-open state");
                }

                // Execute the Redis operation
                await redisOperation(_redis);
                
                // Success - reset failures and close the circuit if needed
                await ResetCircuitAsync();
            }
            catch (RedisConnectionException ex)
            {
                await HandleFailureAsync(ex);
                await fallbackOperation();
            }
            catch (RedisTimeoutException ex)
            {
                await HandleFailureAsync(ex);
                await fallbackOperation();
            }
            catch (Exception ex)
            {
                // Only increment failure count for connection-related exceptions
                if (IsConnectionFailure(ex))
                {
                    await HandleFailureAsync(ex);
                    await fallbackOperation();
                }
                else
                {
                    throw; // Rethrow other exceptions
                }
            }
        }

        private async Task<T> HandleFailureAsync<T>(Exception exception, Func<Task<T>> fallbackOperation)
        {
            await HandleFailureAsync(exception);
            return await fallbackOperation();
        }

        private async Task HandleFailureAsync(Exception exception)
        {
            await _stateChangeLock.WaitAsync();
            try
            {
                _failureCount++;
                _lastFailureTime = DateTime.UtcNow;
                
                _logger.LogWarning(exception, "Redis operation failed. Failure count: {FailureCount}", _failureCount);

                if (_state == CircuitState.HalfOpen || _failureCount >= _options.FailureThreshold)
                {
                    // Open the circuit
                    _state = CircuitState.Open;
                    _circuitOpenTime = DateTime.UtcNow;
                    _logger.LogWarning("Circuit breaker opened at {OpenTime} after {FailureCount} failures", 
                        _circuitOpenTime, _failureCount);
                }

                // Apply exponential backoff for retries
                await ApplyBackoffDelayAsync();
            }
            finally
            {
                _stateChangeLock.Release();
            }
        }
        
        private async Task TryTransitionToHalfOpenAsync()
        {
            await _stateChangeLock.WaitAsync();
            try
            {
                if (_state == CircuitState.Open && DateTime.UtcNow >= _circuitOpenTime.AddSeconds(_options.ResetTimeoutSeconds))
                {
                    _state = CircuitState.HalfOpen;
                    _logger.LogInformation("Circuit transitioned to half-open state at {Time}", DateTime.UtcNow);
                }
            }
            finally
            {
                _stateChangeLock.Release();
            }
        }

        private async Task ResetCircuitAsync()
        {
            await _stateChangeLock.WaitAsync();
            try
            {
                if (_state != CircuitState.Closed)
                {
                    _state = CircuitState.Closed;
                    _logger.LogInformation("Circuit breaker reset to closed state");
                }
                _failureCount = 0;
            }
            finally
            {
                _stateChangeLock.Release();
            }
        }

        private async Task ApplyBackoffDelayAsync()
        {
            // Calculate exponential backoff with jitter
            double backoffMs = Math.Min(
                _options.InitialBackoffMs * Math.Pow(_options.BackoffExponent, _failureCount - 1),
                _options.MaxBackoffSeconds * 1000);
                
            // Add jitter (random 0-20% variation) to prevent thundering herd
            var random = new Random();
            backoffMs = backoffMs * (1 + (random.NextDouble() * 0.2));
            
            _logger.LogDebug("Applying backoff delay of {BackoffMs}ms before next retry", (int)backoffMs);
            await Task.Delay((int)backoffMs);
        }

        private bool IsConnectionFailure(Exception ex)
        {
            // Check for various connection-related exception types
            return ex is RedisConnectionException ||
                   ex is RedisTimeoutException ||
                   ex is SocketException ||
                   (ex.InnerException != null && IsConnectionFailure(ex.InnerException)) ||
                   ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase) ||
                   ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
                   ex.Message.Contains("redis is not available", StringComparison.OrdinalIgnoreCase);
        }
    }
}