using System;
using System.Linq;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Microsoft.Extensions.Logging;

namespace WorkoutTrackerWeb.Services.Hangfire
{
    /// <summary>
    /// Custom attribute that applies an exponential backoff retry strategy for failed Hangfire jobs
    /// </summary>
    public class JobRetryBackoffAttribute : JobFilterAttribute, IElectStateFilter
    {
        private readonly int _maxRetryAttempts;
        private readonly int _initialDelaySeconds;
        private readonly bool _logFailures;
        private readonly ILogger<JobRetryBackoffAttribute> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="JobRetryBackoffAttribute"/> class
        /// </summary>
        /// <param name="maxRetryAttempts">Maximum number of retry attempts before giving up</param>
        /// <param name="initialDelaySeconds">Initial delay in seconds before the first retry</param>
        /// <param name="logFailures">Whether to log detailed failure information</param>
        public JobRetryBackoffAttribute(int maxRetryAttempts = 5, int initialDelaySeconds = 10, bool logFailures = true)
        {
            _maxRetryAttempts = maxRetryAttempts;
            _initialDelaySeconds = initialDelaySeconds;
            _logFailures = logFailures;
            _logger = GetLogger();
        }

        /// <summary>
        /// Called when the state of a job is about to be changed
        /// </summary>
        public void OnStateElection(ElectStateContext context)
        {
            // Only handle the failed state transition
            if (context.CandidateState.Name != FailedState.StateName)
            {
                return;
            }

            var failedState = context.CandidateState as FailedState;
            var retryAttempt = GetRetryAttempt(context.Connection, context.BackgroundJob.Id);

            if (retryAttempt >= _maxRetryAttempts)
            {
                // We've exceeded the maximum retry attempts, let the job fail permanently
                if (_logFailures)
                {
                    _logger?.LogError(failedState?.Exception, 
                        "Job {JobId} ({JobName}) has permanently failed after {RetryAttempt} retry attempts: {ErrorMessage}",
                        context.BackgroundJob.Id,
                        context.BackgroundJob.Job.Method.Name,
                        retryAttempt,
                        failedState?.Exception.Message);
                }
                return;
            }

            // Calculate the delay with exponential backoff
            var delayInSeconds = CalculateBackoffDelay(retryAttempt);

            // Schedule the job to be retried after the backoff delay
            context.CandidateState = new ScheduledState(TimeSpan.FromSeconds(delayInSeconds));

            if (_logFailures)
            {
                _logger?.LogWarning(
                    "Job {JobId} ({JobName}) failed with exception: {Message}. Retrying in {Delay} seconds (attempt {RetryAttempt}/{MaxRetries}).",
                    context.BackgroundJob.Id,
                    context.BackgroundJob.Job.Method.Name,
                    failedState?.Exception.Message,
                    delayInSeconds,
                    retryAttempt + 1,
                    _maxRetryAttempts);
            }
        }

        /// <summary>
        /// Gets the current retry attempt number from Hangfire's storage
        /// </summary>
        private int GetRetryAttempt(IStorageConnection connection, string jobId)
        {
            try
            {
                // Get job data and check if it exists
                var jobData = connection.GetJobData(jobId);
                if (jobData == null) return 0;

                // Count historical states that are "failed" states to determine retry count
                var stateHistory = JobStorage.Current.GetMonitoringApi().JobDetails(jobId)?.History;
                if (stateHistory == null || stateHistory.Count == 0) return 0;
                
                return stateHistory.Count(x => x.StateName == FailedState.StateName);
            }
            catch (Exception)
            {
                // If we can't get the job history for any reason, assume it's the first attempt
                return 0;
            }
        }

        /// <summary>
        /// Calculate the delay before the next retry using exponential backoff
        /// </summary>
        private double CalculateBackoffDelay(int retryAttempt)
        {
            // Calculate exponential backoff with jitter to prevent retry storms
            // Formula: initialDelay * (2^retryAttempt) with 20% random jitter
            var backoffMultiplier = Math.Pow(2, retryAttempt);
            var exactDelay = _initialDelaySeconds * backoffMultiplier;
            
            // Add jitter by randomly adjusting by +/- 20%
            var random = new Random();
            var jitter = (random.NextDouble() * 0.4) - 0.2; // -0.2 to +0.2
            var jitteredDelay = exactDelay * (1.0 + jitter);

            // Cap the maximum delay at 30 minutes (1800 seconds)
            return Math.Min(jitteredDelay, 1800);
        }

        /// <summary>
        /// Gets a logger instance for the attribute
        /// </summary>
        private ILogger<JobRetryBackoffAttribute> GetLogger()
        {
            try
            {
                // Try to get a logger from the application's service provider
                var serviceProvider = JobFilterAttributeUtils.GetServiceProvider();
                return serviceProvider?.GetService(typeof(ILogger<JobRetryBackoffAttribute>)) as ILogger<JobRetryBackoffAttribute>;
            }
            catch
            {
                // Fallback to null if we can't get the logger
                return null;
            }
        }
    }

    /// <summary>
    /// Helper class for job filter attributes to access services
    /// </summary>
    public static class JobFilterAttributeUtils
    {
        private static IServiceProvider _serviceProvider;

        public static void SetServiceProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public static IServiceProvider GetServiceProvider()
        {
            return _serviceProvider;
        }
    }
}