using System;
using System.Collections.Generic;

namespace WorkoutTrackerWeb.TempModel;

public partial class WorkoutFeedback
{
    public int WorkoutFeedbackId { get; set; }

    public int SessionId { get; set; }

    public int ClientUserId { get; set; }

    public DateTime FeedbackDate { get; set; }

    public int OverallRating { get; set; }

    public int DifficultyRating { get; set; }

    public int EnergyLevel { get; set; }

    public string Comments { get; set; }

    public bool CompletedAllExercises { get; set; }

    public string IncompleteReason { get; set; }

    public bool CoachNotified { get; set; }

    public bool CoachViewed { get; set; }
}
