using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Serilog.Events;

namespace WorkoutTrackerWeb.Models.Logging
{
    public class LogLevelSettings
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Default Log Level")]
        public LogEventLevel DefaultLogLevel { get; set; } = LogEventLevel.Information;

        [Required]
        [Display(Name = "Last Updated")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        [Required]
        [Display(Name = "Last Updated By")]
        [StringLength(256)]
        public string LastUpdatedBy { get; set; } = "System";

        // Store override settings for specific namespaces/sources
        public List<LogLevelOverride> Overrides { get; set; } = new List<LogLevelOverride>();
    }

    public class LogLevelOverride
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Source Context")]
        [StringLength(256)]
        public string SourceContext { get; set; }

        [Required]
        [Display(Name = "Log Level")]
        public LogEventLevel LogLevel { get; set; }

        // Navigation property
        public int LogLevelSettingsId { get; set; }
        public LogLevelSettings LogLevelSettings { get; set; }
    }
}