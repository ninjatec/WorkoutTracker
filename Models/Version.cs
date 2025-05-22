using System;
using System.ComponentModel.DataAnnotations;

namespace WorkoutTrackerWeb.Models
{
    public class AppVersion
    {
        [Key]
        public int VersionId { get; set; }
        
        [Required]
        public int Major { get; set; }
        
        [Required]
        public int Minor { get; set; }
        
        [Required]
        public int Patch { get; set; }
        
        [Required]
        public int BuildNumber { get; set; }
        
        [Required]
        public DateTime ReleaseDate { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Description { get; set; }
        
        [StringLength(40)]
        public string GitCommitHash { get; set; }
        
        public bool IsCurrent { get; set; }
        
        [StringLength(255)]
        public string ReleaseNotes { get; set; }
        
        // Helper methods to get the version as a string
        public string GetVersionString()
        {
            return $"{Major}.{Minor}.{Patch}.{BuildNumber}";
        }
        
        // Format with optional git hash
        public string GetFullVersionString()
        {
            if (!string.IsNullOrEmpty(GitCommitHash))
            {
                return $"{GetVersionString()} ({GitCommitHash.Substring(0, Math.Min(GitCommitHash.Length, 8))})";
            }
            return GetVersionString();
        }
    }
}