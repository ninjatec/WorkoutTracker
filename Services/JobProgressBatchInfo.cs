using System;

namespace WorkoutTrackerWeb.Services
{
    /// <summary>
    /// Provides information about batched messages for job progress updates
    /// </summary>
    public class JobProgressBatchInfo
    {
        /// <summary>
        /// The number of updates in this batch
        /// </summary>
        public int BatchSize { get; set; }
        
        /// <summary>
        /// Whether this progress update represents multiple batched updates
        /// </summary>
        public bool BatchedUpdates { get; set; }
        
        /// <summary>
        /// Timestamp when this batch was created
        /// </summary>
        public DateTime BatchTimestamp { get; set; } = DateTime.UtcNow;
    }
}