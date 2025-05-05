using System;

namespace WorkoutTrackerWeb.Models
{
    /// <summary>
    /// Client-side configuration for job progress updates
    /// </summary>
    public class JobProgressClientConfig
    {
        /// <summary>
        /// Minimum time between UI updates in milliseconds
        /// </summary>
        public int MinimumUiUpdateIntervalMs { get; set; } = 100;

        /// <summary>
        /// Whether to enable client-side console debugging
        /// </summary>
        public bool EnableConsoleDebug { get; set; } = false;
    }

    /// <summary>
    /// Information about a batch of progress updates
    /// </summary>
    public class JobProgressBatchInfo
    {
        /// <summary>
        /// Unique identifier for this batch
        /// </summary>
        public string BatchId { get; set; }

        /// <summary>
        /// Timestamp when the batch was created
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Number of updates that were skipped and not sent to clients
        /// </summary>
        public int ItemsSkipped { get; set; }

        /// <summary>
        /// Whether this update is part of a batch
        /// </summary>
        public bool IsPartOfBatch { get; set; }

        /// <summary>
        /// Client-side configuration for rendering progress updates
        /// </summary>
        public JobProgressClientConfig ClientConfig { get; set; }
    }
}