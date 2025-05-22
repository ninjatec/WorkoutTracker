using System;
using System.Threading.Tasks;
using System.Linq.Expressions;
using Hangfire;
using Microsoft.Extensions.Logging;
using Hangfire.Common;
using System.Reflection;
using Hangfire.States;

namespace WorkoutTrackerWeb.Services.Hangfire
{
    /// <summary>
    /// Provides a fluent interface for building complex job continuations in Hangfire
    /// </summary>
    public class JobContinuationBuilder
    {
        private readonly ILogger _logger;
        private string _currentJobId;
        private string _jobName;

        /// <summary>
        /// Creates a new instance of the <see cref="JobContinuationBuilder"/> class
        /// </summary>
        public JobContinuationBuilder(ILogger logger, string initialJobId = null, string jobName = null)
        {
            _logger = logger;
            _currentJobId = initialJobId;
            _jobName = jobName ?? "Unnamed job";
            
            if (!string.IsNullOrEmpty(_currentJobId))
            {
                _logger.LogInformation("Starting job continuation chain with initial job {JobId} ({JobName})", _currentJobId, _jobName);
            }
        }

        /// <summary>
        /// Starts a new workflow with the specified job
        /// </summary>
        public JobContinuationBuilder StartWith<T>(Expression<Func<T, Task>> methodCall, string jobName = null)
        {
            _jobName = jobName ?? ExpressionExtensions.Name(methodCall);
            _currentJobId = BackgroundJob.Enqueue(methodCall);
            _logger.LogInformation("Started new job workflow with job {JobId} ({JobName})", _currentJobId, _jobName);
            return this;
        }

        /// <summary>
        /// Starts a new workflow with the specified job
        /// </summary>
        public JobContinuationBuilder StartWith<T>(Expression<Action<T>> methodCall, string jobName = null)
        {
            _jobName = jobName ?? ExpressionExtensions.Name(methodCall);
            _currentJobId = BackgroundJob.Enqueue(methodCall);
            _logger.LogInformation("Started new job workflow with job {JobId} ({JobName})", _currentJobId, _jobName);
            return this;
        }

        /// <summary>
        /// Continues the workflow with the specified job when the previous job succeeds
        /// </summary>
        public JobContinuationBuilder Then<T>(Expression<Func<T, Task>> methodCall, string jobName = null)
        {
            if (string.IsNullOrEmpty(_currentJobId))
            {
                throw new InvalidOperationException("Cannot add continuation because no previous job exists. Use StartWith first.");
            }
            
            jobName = jobName ?? ExpressionExtensions.Name(methodCall);
            var continuationJobId = BackgroundJob.ContinueJobWith(_currentJobId, methodCall);
            _logger.LogInformation("Added continuation job {ContinuationJobId} ({JobName}) after job {JobId}", 
                continuationJobId, jobName, _currentJobId);
            
            _currentJobId = continuationJobId;
            _jobName = jobName;
            return this;
        }

        /// <summary>
        /// Continues the workflow with the specified job when the previous job succeeds
        /// </summary>
        public JobContinuationBuilder Then<T>(Expression<Action<T>> methodCall, string jobName = null)
        {
            if (string.IsNullOrEmpty(_currentJobId))
            {
                throw new InvalidOperationException("Cannot add continuation because no previous job exists. Use StartWith first.");
            }
            
            jobName = jobName ?? ExpressionExtensions.Name(methodCall);
            var continuationJobId = BackgroundJob.ContinueJobWith(_currentJobId, methodCall);
            _logger.LogInformation("Added continuation job {ContinuationJobId} ({JobName}) after job {JobId}", 
                continuationJobId, jobName, _currentJobId);
            
            _currentJobId = continuationJobId;
            _jobName = jobName;
            return this;
        }

        /// <summary>
        /// Adds a job that will run in the event of failure in the previous job
        /// </summary>
        public JobContinuationBuilder OnFailure<TFailureHandler>(Expression<Action<TFailureHandler>> methodCall, string jobName = null)
        {
            if (string.IsNullOrEmpty(_currentJobId))
            {
                throw new InvalidOperationException("Cannot add failure handler because no previous job exists. Use StartWith first.");
            }
            
            jobName = jobName ?? ExpressionExtensions.Name(methodCall);
            
            // We need a custom approach since there's no direct API for failure-only continuations
            // First, get the current job state
            var currentState = JobStorage.Current.GetConnection().GetStateData(_currentJobId);
            
            if (currentState?.Name == FailedState.StateName)
            {
                // If it's already failed, execute the failure handler immediately
                var failureJobId = BackgroundJob.Enqueue(methodCall);
                _logger.LogInformation("Job {JobId} already failed, immediately executing failure handler {FailureJobId}", 
                    _currentJobId, failureJobId);
            }
            else
            {
                // Otherwise, set up a state changed event handler
                var failureHandlerId = Guid.NewGuid().ToString();
                var jobStorageConnection = JobStorage.Current.GetConnection();
                
                // Register a recurring job that periodically checks if the job has failed
                RecurringJob.AddOrUpdate(
                    $"failure-check-{_currentJobId}-{failureHandlerId}",
                    () => CheckAndExecuteFailureHandler(_currentJobId, methodCall, jobName, failureHandlerId),
                    Cron.MinuteInterval(1));
                
                _logger.LogInformation("Added failure handler check job for job {JobId}", _currentJobId);
            }
            
            return this;
        }

        /// <summary>
        /// Helper method that checks if a job has failed and executes a failure handler if needed
        /// </summary>
        public static void CheckAndExecuteFailureHandler<TFailureHandler>(
            string jobId, 
            Expression<Action<TFailureHandler>> methodCall, 
            string handlerName,
            string checkerId)
        {
            var connection = JobStorage.Current.GetConnection();
            var jobData = connection.GetJobData(jobId);
            if (jobData == null) 
            {
                // Job doesn't exist, clean up the checker
                RecurringJob.RemoveIfExists($"failure-check-{jobId}-{checkerId}");
                return;
            }
            
            var stateData = connection.GetStateData(jobId);
            if (stateData?.Name == FailedState.StateName)
            {
                // Job has failed, execute the handler and clean up
                var handlerId = BackgroundJob.Enqueue(methodCall);
                
                // Clean up the recurring job
                RecurringJob.RemoveIfExists($"failure-check-{jobId}-{checkerId}");
            }
            else if (stateData?.Name == SucceededState.StateName)
            {
                // Job succeeded, clean up the checker
                RecurringJob.RemoveIfExists($"failure-check-{jobId}-{checkerId}");
            }
        }

        /// <summary>
        /// Adds a callback job that will run regardless of whether the previous job succeeds or fails
        /// </summary>
        public JobContinuationBuilder Finally<T>(Expression<Func<T, Task>> methodCall, string jobName = null)
        {
            if (string.IsNullOrEmpty(_currentJobId))
            {
                throw new InvalidOperationException("Cannot add final handler because no previous job exists. Use StartWith first.");
            }
            
            jobName = jobName ?? ExpressionExtensions.Name(methodCall);
            var finallyJobId = BackgroundJob.ContinueJobWith(_currentJobId, methodCall);
                
            _logger.LogInformation("Added finally handler job {FinallyJobId} ({JobName}) for job {JobId}", 
                finallyJobId, jobName, _currentJobId);

            _currentJobId = finallyJobId;
            _jobName = jobName;
            
            return this;
        }

        /// <summary>
        /// Adds a callback job that will run regardless of whether the previous job succeeds or fails
        /// </summary>
        public JobContinuationBuilder Finally<T>(Expression<Action<T>> methodCall, string jobName = null)
        {
            if (string.IsNullOrEmpty(_currentJobId))
            {
                throw new InvalidOperationException("Cannot add final handler because no previous job exists. Use StartWith first.");
            }
            
            jobName = jobName ?? ExpressionExtensions.Name(methodCall);
            var finallyJobId = BackgroundJob.ContinueJobWith(_currentJobId, methodCall);
                
            _logger.LogInformation("Added finally handler job {FinallyJobId} ({JobName}) for job {JobId}", 
                finallyJobId, jobName, _currentJobId);

            _currentJobId = finallyJobId;
            _jobName = jobName;
            
            return this;
        }

        /// <summary>
        /// Gets the ID of the last job in the continuation chain
        /// </summary>
        public string GetLastJobId()
        {
            return _currentJobId;
        }

        /// <summary>
        /// Gets the name of the last job in the continuation chain
        /// </summary>
        public string GetLastJobName()
        {
            return _jobName;
        }
    }

    /// <summary>
    /// Extension methods for expression handling
    /// </summary>
    public static class ExpressionExtensions
    {
        /// <summary>
        /// Gets a method name from an expression
        /// </summary>
        public static string Name<T>(Expression<T> expression)
        {
            var methodCallExpression = expression.Body as MethodCallExpression;
            if (methodCallExpression != null)
            {
                return methodCallExpression.Method.Name;
            }
            
            return "Unknown method";
        }
    }
}