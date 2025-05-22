using System;
using System.Collections.Generic;

namespace WorkoutTrackerWeb.Models
{
    /// <summary>
    /// Status of a background job
    /// </summary>
    public enum JobStatus
    {
        Queued,
        Starting,
        InProgress,
        Completed,
        Failed,
        Canceled
    }

    /// <summary>
    /// Represents the progress of a background job
    /// </summary>
    public class JobProgress
    {
        /// <summary>
        /// The unique identifier of the job
        /// </summary>
        public string JobId { get; set; }

        /// <summary>
        /// Current status of the job
        /// </summary>
        public JobStatus Status { get; set; }

        /// <summary>
        /// Percent complete (0-100)
        /// </summary>
        public int PercentComplete { get; set; }

        /// <summary>
        /// Current processing stage description
        /// </summary>
        public string CurrentStage { get; set; }

        /// <summary>
        /// The total number of items to process
        /// </summary>
        public int TotalItems { get; set; }

        /// <summary>
        /// The number of items processed so far
        /// </summary>
        public int ProcessedItems { get; set; }

        /// <summary>
        /// The number of successfully processed items
        /// </summary>
        public int SuccessItems { get; set; }

        /// <summary>
        /// The number of items that couldn't be processed
        /// </summary>
        public int FailedItems { get; set; }

        /// <summary>
        /// The detailed error message if the job failed
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Detailed error information, where available
        /// </summary>
        public List<string> DetailedErrors { get; set; } = new List<string>();

        /// <summary>
        /// Timestamp when the job started
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// Timestamp when the job completed or failed
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Estimated time remaining in seconds
        /// </summary>
        public double? EstimatedSecondsRemaining { get; set; }

        /// <summary>
        /// Result URL to redirect to when job completes
        /// </summary>
        public string ResultUrl { get; set; }

        /// <summary>
        /// Custom properties for job-specific data
        /// </summary>
        public Dictionary<string, object> CustomData { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Information about the batch this progress update is part of
        /// </summary>
        public JobProgressBatchInfo BatchInfo { get; set; }
        
        /// <summary>
        /// Creation time for this progress report
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}