using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using WorkoutTrackerWeb.Models.Validation;

namespace WorkoutTrackerWeb.ViewModels.Coaching
{
    /// <summary>
    /// View model for client group creation and editing
    /// </summary>
    public class ClientGroupViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Group name is required")]
        [GroupName]
        [Display(Name = "Group Name")]
        public string Name { get; set; }

        [GroupDescription]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [Display(Name = "Color")]
        public string ColorCode { get; set; } = "#0d6efd"; // Default blue color

        public DateTime? CreatedDate { get; set; }
        public DateTime? LastModifiedDate { get; set; }

        public List<int> SelectedClients { get; set; } = new List<int>();
    }

    /// <summary>
    /// View model for client invitation
    /// </summary>
    public class ClientInvitationViewModel
    {
        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Client Email")]
        public string ClientEmail { get; set; }

        [InvitationMessage]
        [Display(Name = "Invitation Message")]
        public string InvitationMessage { get; set; }

        [Range(1, 90, ErrorMessage = "Expiry days must be between 1 and 90")]
        [Display(Name = "Invitation Expires After")]
        public int ExpiryDays { get; set; } = 14;

        [Display(Name = "Permissions")]
        public List<string> Permissions { get; set; } = new List<string>() {
            "canViewWorkouts", "canCreateWorkouts", "canEditWorkouts",
            "canViewReports", "canCreateGoals"
        };
    }

    /// <summary>
    /// View model for client goal creation and editing
    /// </summary>
    public class ClientGoalViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Goal description is required")]
        [GoalDescription]
        [Display(Name = "Goal Description")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Start date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Target date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Target Date")]
        public DateTime TargetDate { get; set; } = DateTime.Today.AddDays(30);

        [Display(Name = "Goal Type")]
        public string GoalType { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; } = "Active";

        [Display(Name = "Client ID")]
        public int ClientRelationshipId { get; set; }
    }

    /// <summary>
    /// View model for coaching notes
    /// </summary>
    public class CoachingNoteViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Note content is required")]
        [CoachingNote]
        [Display(Name = "Note")]
        public string Content { get; set; }

        [Display(Name = "Visible to Client")]
        public bool IsVisibleToClient { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Client")]
        public int ClientRelationshipId { get; set; }
    }

    /// <summary>
    /// View model for template assignments
    /// </summary>
    public class TemplateAssignmentViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Template is required")]
        [Display(Name = "Template")]
        public int TemplateId { get; set; }

        [Required(ErrorMessage = "Assignment name is required")]
        [TemplateName]
        [Display(Name = "Assignment Name")]
        public string Name { get; set; }

        [Display(Name = "Notes")]
        public string Notes { get; set; }

        [Display(Name = "Client")]
        public int ClientRelationshipId { get; set; }
        
        [Display(Name = "Assign to Group")]
        public int? GroupId { get; set; }

        public List<int> SelectedClients { get; set; } = new List<int>();
    }
}