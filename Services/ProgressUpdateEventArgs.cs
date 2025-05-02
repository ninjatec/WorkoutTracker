using System;

namespace WorkoutTrackerWeb.Services
{
    /// <summary>
    /// Event arguments for progress updates in long-running operations
    /// </summary>
    public class ProgressUpdateEventArgs : EventArgs
    {
        public int PercentComplete { get; set; }
        public string Message { get; set; }
    }
}