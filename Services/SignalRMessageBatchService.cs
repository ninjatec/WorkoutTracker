using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WorkoutTrackerWeb.Hubs;
using WorkoutTrackerWeb.Models;

namespace WorkoutTrackerWeb.Services
{
    /// <summary>
    /// Configuration options for SignalR message batching
    /// </summary>
    public class SignalRBatchOptions
    {
        /// <summary>
        /// How often to process the batch queue in milliseconds
        /// </summary>
        public int BatchIntervalMs { get; set; } = 1000;

        /// <summary>
        /// Maximum number of messages to process in a single batch
        /// </summary>
        public int MaxBatchSize { get; set; } = 25;

        /// <summary>
        /// Minimum time between UI updates for high-frequency events on the client side
        /// </summary>
        public int MinimumClientUiUpdateIntervalMs { get; set; } = 100;
    }

    /// <summary>
    /// Service for batching and throttling high-frequency SignalR messages
    /// </summary>
    public class SignalRMessageBatchService : IHostedService, IDisposable
    {
        private readonly ILogger<SignalRMessageBatchService> _logger;
        private readonly IHubContext<ImportProgressHub> _hubContext;
        private readonly SignalRBatchOptions _options;

        // Stores the latest progress for each job ID
        private readonly ConcurrentDictionary<string, Models.JobProgress> _latestProgressByJob = new();

        // Store all progress updates in a queue by job ID
        private readonly ConcurrentDictionary<string, ConcurrentQueue<Models.JobProgress>> _progressQueue = new();

        // Timer to process batches
        private Timer _batchTimer;

        // Flag to track if processing is currently happening
        private int _processingBatch = 0;

        // Store the last time a batch was sent for each job
        private readonly ConcurrentDictionary<string, DateTime> _lastBatchSentTime = new();

        public SignalRMessageBatchService(
            ILogger<SignalRMessageBatchService> logger,
            IHubContext<ImportProgressHub> hubContext,
            IOptions<SignalRBatchOptions> options)
        {
            _logger = logger;
            _hubContext = hubContext;
            _options = options.Value;
        }

        /// <summary>
        /// Queue a progress update for batched sending
        /// </summary>
        /// <param name="jobId">The job ID</param>
        /// <param name="progress">The progress information</param>
        public void QueueProgressUpdate(string jobId, Models.JobProgress progress)
        {
            if (string.IsNullOrEmpty(jobId) || progress == null)
            {
                return;
            }

            // Create batch info if not present
            if (progress.BatchInfo == null)
            {
                progress.BatchInfo = new Models.JobProgressBatchInfo
                {
                    BatchId = Guid.NewGuid().ToString(),
                    Timestamp = DateTime.UtcNow,
                    IsPartOfBatch = true,
                    ClientConfig = new Models.JobProgressClientConfig
                    {
                        MinimumUiUpdateIntervalMs = _options.MinimumClientUiUpdateIntervalMs
                    }
                };
            }
            else
            {
                // Update timestamp
                progress.BatchInfo.Timestamp = DateTime.UtcNow;
                progress.BatchInfo.IsPartOfBatch = true;
            }

            // Store as the latest progress
            _latestProgressByJob[jobId] = progress;

            // Add to the queue for this job
            var queue = _progressQueue.GetOrAdd(jobId, _ => new ConcurrentQueue<Models.JobProgress>());
            queue.Enqueue(progress);

            _logger.LogTrace("Queued progress update for job {JobId}: {Status} {PercentComplete}%", 
                jobId, progress.Status, progress.PercentComplete);
        }

        /// <summary>
        /// Get the latest progress for a job
        /// </summary>
        /// <param name="jobId">The job ID</param>
        /// <returns>The latest progress or null if no updates available</returns>
        public Models.JobProgress GetLatestProgressForJob(string jobId)
        {
            if (string.IsNullOrEmpty(jobId))
            {
                return null;
            }

            _latestProgressByJob.TryGetValue(jobId, out var progress);
            return progress;
        }

        /// <summary>
        /// Process batched updates for all jobs
        /// </summary>
        private async Task ProcessBatchesAsync(CancellationToken cancellationToken)
        {
            // Use Interlocked to ensure only one thread processes batches at a time
            if (Interlocked.CompareExchange(ref _processingBatch, 1, 0) != 0)
            {
                return; // Already processing
            }

            try
            {
                foreach (var jobId in _progressQueue.Keys)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    await ProcessJobBatchAsync(jobId, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing SignalR message batches");
            }
            finally
            {
                Interlocked.Exchange(ref _processingBatch, 0);
            }
        }

        /// <summary>
        /// Process batched updates for a specific job
        /// </summary>
        private async Task ProcessJobBatchAsync(string jobId, CancellationToken cancellationToken)
        {
            if (!_progressQueue.TryGetValue(jobId, out var queue) || queue.IsEmpty)
            {
                return;
            }

            // Check if enough time has passed since the last batch send
            _lastBatchSentTime.TryGetValue(jobId, out DateTime lastSent);
            var now = DateTime.UtcNow;
            var timeSinceLastSend = now - lastSent;
            
            // Skip if we just sent a batch and there's not much queued
            if (timeSinceLastSend.TotalMilliseconds < _options.BatchIntervalMs && queue.Count < 5)
            {
                return;
            }

            _lastBatchSentTime[jobId] = now;

            // Get the latest progress update
            if (!_latestProgressByJob.TryGetValue(jobId, out var latestProgress))
            {
                return;
            }

            // Update batch info
            latestProgress.BatchInfo ??= new Models.JobProgressBatchInfo();
            latestProgress.BatchInfo.BatchId = Guid.NewGuid().ToString();
            latestProgress.BatchInfo.Timestamp = now;
            latestProgress.BatchInfo.ItemsSkipped = Math.Max(0, queue.Count - 1);
            latestProgress.BatchInfo.IsPartOfBatch = queue.Count > 1;
            latestProgress.BatchInfo.ClientConfig ??= new Models.JobProgressClientConfig
            {
                MinimumUiUpdateIntervalMs = _options.MinimumClientUiUpdateIntervalMs
            };

            // Clear the queue - we only send the latest update
            while (queue.TryDequeue(out _)) { /* Empty the queue */ }

            try
            {
                // Send to the job group
                string groupName = $"job_{jobId}";
                await _hubContext.Clients.Group(groupName).SendAsync("receiveProgress", latestProgress, cancellationToken);
                
                _logger.LogDebug("Sent batched update to job group {JobId}: {Status} {PercentComplete}% (skipped {SkippedCount} updates)",
                    jobId, latestProgress.Status, latestProgress.PercentComplete, latestProgress.BatchInfo.ItemsSkipped);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending batched update to job group {JobId}", jobId);
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting SignalR message batch service with interval {BatchIntervalMs}ms",
                _options.BatchIntervalMs);

            // Set up timer to process batches periodically
            _batchTimer = new Timer(
                async _ => await ProcessBatchesAsync(cancellationToken), 
                null, 
                TimeSpan.Zero, 
                TimeSpan.FromMilliseconds(_options.BatchIntervalMs));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping SignalR message batch service");
            _batchTimer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _batchTimer?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}