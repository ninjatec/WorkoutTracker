using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Microsoft.Data.SqlClient;

namespace WorkoutTrackerWeb.Services
{
    /// <summary>
    /// Provides resilient database operations with circuit breaker pattern implementation
    /// to prevent cascading failures when the database is experiencing issues.
    /// </summary>
    public class DatabaseResilienceService
    {
        private readonly ILogger<DatabaseResilienceService> _logger;
        private readonly AsyncCircuitBreakerPolicy _circuitBreakerPolicy;
        private readonly AsyncRetryPolicy _retryPolicy;
        
        // Circuit breaker state tracking
        private bool _isCircuitBreakerOpen = false;
        private DateTime? _circuitBreakerLastStateChange;

        public DatabaseResilienceService(ILogger<DatabaseResilienceService> logger, IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            var poolingConfig = configuration.GetSection("DatabaseConnectionPooling");
            int retryCount = poolingConfig.GetValue<int>("RetryCount", 3);
            int retryInterval = poolingConfig.GetValue<int>("RetryInterval", 10);
            int circuitBreakerThreshold = poolingConfig.GetValue<int>("CircuitBreakerThreshold", 5);
            int circuitBreakerDelay = poolingConfig.GetValue<int>("CircuitBreakerDelay", 30);
            
            // Configure retry policy with exponential backoff using config values
            _retryPolicy = Policy
                .Handle<SqlException>(ex => IsTransientError(ex))
                .Or<TimeoutException>()
                .WaitAndRetryAsync(
                    retryCount: retryCount,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(retryInterval * Math.Pow(1.5, retryAttempt - 1)),
                    onRetry: (exception, timeSpan, attemptCount, context) =>
                    {
                        _logger.LogWarning(exception, 
                            "Database operation failed. Retry attempt {RetryCount} after {RetryTimeSpan}ms delay", 
                            attemptCount, timeSpan.TotalMilliseconds);
                    }
                );
            
            // Configure circuit breaker policy using config values
            _circuitBreakerPolicy = Policy
                .Handle<SqlException>(ex => IsTransientError(ex))
                .Or<TimeoutException>()
                .CircuitBreakerAsync(
                    exceptionsAllowedBeforeBreaking: circuitBreakerThreshold,
                    durationOfBreak: TimeSpan.FromSeconds(circuitBreakerDelay),
                    onBreak: (exception, breakDelay) =>
                    {
                        _isCircuitBreakerOpen = true;
                        _circuitBreakerLastStateChange = DateTime.UtcNow;
                        _logger.LogError(exception, 
                            "Circuit breaker opened. Database connections suspended for {BreakDelay}ms.", 
                            breakDelay.TotalMilliseconds);
                    },
                    onReset: () =>
                    {
                        _isCircuitBreakerOpen = false;
                        _circuitBreakerLastStateChange = DateTime.UtcNow;
                        _logger.LogInformation("Circuit breaker reset. Database connections resumed.");
                    },
                    onHalfOpen: () =>
                    {
                        _logger.LogInformation("Circuit breaker half-open. Testing database connectivity.");
                    }
                );
        }

        /// <summary>
        /// Executes a database operation with retry and circuit breaker policies
        /// </summary>
        /// <typeparam name="T">The return type of the database operation</typeparam>
        /// <param name="operation">The database operation to execute</param>
        /// <param name="operationName">Name of the operation for logging</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>The result of the operation</returns>
        public async Task<T> ExecuteWithResilienceAsync<T>(
            Func<CancellationToken, Task<T>> operation, 
            string operationName,
            CancellationToken cancellationToken = default)
        {
            return await _retryPolicy
                .WrapAsync(_circuitBreakerPolicy)
                .ExecuteAsync(async (ct) => {
                    _logger.LogDebug("Executing database operation: {OperationName}", operationName);
                    var result = await operation(ct);
                    _logger.LogDebug("Successfully completed database operation: {OperationName}", operationName);
                    return result;
                }, cancellationToken);
        }

        /// <summary>
        /// Executes a database operation with retry and circuit breaker policies
        /// </summary>
        /// <param name="operation">The database operation to execute</param>
        /// <param name="operationName">Name of the operation for logging</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        public async Task ExecuteWithResilienceAsync(
            Func<CancellationToken, Task> operation, 
            string operationName,
            CancellationToken cancellationToken = default)
        {
            await _retryPolicy
                .WrapAsync(_circuitBreakerPolicy)
                .ExecuteAsync(async (ct) => {
                    _logger.LogDebug("Executing database operation: {OperationName}", operationName);
                    await operation(ct);
                    _logger.LogDebug("Successfully completed database operation: {OperationName}", operationName);
                }, cancellationToken);
        }

        /// <summary>
        /// Gets the current circuit breaker state for health reporting
        /// </summary>
        public (bool isOpen, DateTime? lastStateChange) GetCircuitBreakerState()
        {
            return (_isCircuitBreakerOpen, _circuitBreakerLastStateChange);
        }
        
        /// <summary>
        /// Determines if a SQL exception is transient and can be retried
        /// </summary>
        private bool IsTransientError(SqlException ex)
        {
            // SQL error codes that are considered transient:
            // 4060: Cannot open database
            // 40197: Error processing request. Retry request.
            // 40501: The service is currently busy
            // 40613: Database is currently unavailable
            // 49918: Cannot process request. Not enough resources
            // 4221: Login failed due to timeout
            // 1205: Deadlock victim
            // 233: Connection terminated
            // 64: SQL Server terminated
            // -2: Timeout
            int[] transientErrorCodes = { 4060, 40197, 40501, 40613, 49918, 4221, 1205, 233, 64, -2 };
            
            return Array.IndexOf(transientErrorCodes, ex.Number) >= 0;
        }
    }
}