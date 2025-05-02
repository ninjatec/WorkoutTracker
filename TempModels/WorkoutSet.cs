using System;
using System.Collections.Generic;

namespace WorkoutTrackerWeb.TempModels;

public partial class WorkoutSet
{
    public int WorkoutSetId { get; set; }

    public int WorkoutExerciseId { get; set; }

    public int? SettypeId { get; set; }

    public int SequenceNum { get; set; }

    public int SetNumber { get; set; }

    public int? Reps { get; set; }

    public int? TargetMinReps { get; set; }

    public int? TargetMaxReps { get; set; }

    public decimal? Weight { get; set; }

    public int? DurationSeconds { get; set; }

    public decimal? Distance { get; set; }

    public int? Rpe { get; set; }

    public int? RestSeconds { get; set; }

    public bool IsCompleted { get; set; }

    public string Notes { get; set; }

    public DateTime Timestamp { get; set; }

    public int? Intensity { get; set; }
}
