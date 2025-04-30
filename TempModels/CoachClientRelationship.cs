using System;
using System.Collections.Generic;

namespace WorkoutTrackerWeb.TempModels;

public partial class CoachClientRelationship
{
    public int Id { get; set; }

    public string CoachId { get; set; }

    public string ClientId { get; set; }

    public int Status { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime LastModifiedDate { get; set; }

    public string AppUserId { get; set; }

    public string AppUserId1 { get; set; }

    public int? ClientGroupId { get; set; }

    public DateTime? InvitationExpiryDate { get; set; }

    public string InvitationToken { get; set; }

    public string InvitedEmail { get; set; }
}
