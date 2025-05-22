using System;
using System.Collections.Generic;

namespace WorkoutTrackerWeb.Models
{
    public class WorkoutExport
    {
        public string Version { get; set; } = "1.0";
        public DateTime ExportDate { get; set; }
        public UserExport User { get; set; }
        public List<SessionExport> Sessions { get; set; }
        public List<ExerciseTypeExport> ExerciseTypes { get; set; }
        public List<SetTypeExport> SetTypes { get; set; }
    }

    public class UserExport
    {
        public string Name { get; set; }
        public string Email { get; set; }
    }

    // SessionExport is a legacy export class. All new exports should use WorkoutSession as the source.
    // This class is retained only for backward compatibility with old exports. Remove when all consumers are migrated.
    public class SessionExport
    {
        public string Name { get; set; }
        public DateTime DateTime { get; set; }
        public List<SetExport> Sets { get; set; }
    }

    public class SetExport
    {
        public string Description { get; set; }
        public string Notes { get; set; }
        public string ExerciseTypeName { get; set; }
        public string SetTypeName { get; set; }
        public int NumberReps { get; set; }
        public decimal Weight { get; set; }
        public List<RepExport> Reps { get; set; }
    }

    public class RepExport
    {
        public decimal Weight { get; set; }
        public int RepNumber { get; set; }
        public bool Success { get; set; }
    }

    public class ExerciseTypeExport
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class SetTypeExport
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }
}