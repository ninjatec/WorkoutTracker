using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WorkoutTrackerWeb.Models;

namespace WorkoutTrackerWeb.Services.VersionManagement
{
    public interface IVersionService
    {
        /// <summary>
        /// Gets the current version of the application
        /// </summary>
        /// <returns>The current version</returns>
        Task<AppVersion> GetCurrentVersionAsync();

        /// <summary>
        /// Gets the full version history
        /// </summary>
        /// <returns>All versions in the system</returns>
        Task<IEnumerable<AppVersion>> GetVersionHistoryAsync();

        /// <summary>
        /// Updates the current version
        /// </summary>
        /// <returns>The new current version</returns>
        Task<AppVersion> UpdateVersionAsync(int major, int minor, int patch, int build, string description, string gitCommitHash = null);

        /// <summary>
        /// Adds a version to the history without marking it current
        /// </summary>
        /// <returns>The newly added version</returns>
        Task<AppVersion> AddVersionHistoryAsync(int major, int minor, int patch, int build, 
            string description, string gitCommitHash = null, string releaseNotes = null);

        /// <summary>
        /// Gets a formatted string of the current version
        /// </summary>
        /// <returns>String representation of the current version</returns>
        string GetVersionDisplayString();

        /// <summary>
        /// Gets a formatted string of the current version asynchronously
        /// </summary>
        /// <returns>String representation of the current version</returns>
        Task<string> GetVersionDisplayStringAsync();
    }
}