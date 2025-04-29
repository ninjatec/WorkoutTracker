using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkoutTrackerWeb.Models.Coaching
{
    /// <summary>
    /// Represents a scheduled workout for a client
    /// </summary>
    public class WorkoutSchedule
    {
        public int WorkoutScheduleId { get; set; }
        
        // Foreign key for template assignment
        public int? TemplateAssignmentId { get; set; }
        
        // Foreign key for direct template reference (for self-scheduling)
        public int? TemplateId { get; set; }
        
        // Foreign key for client User
        public int ClientUserId { get; set; }
        
        // Foreign key for coach User
        public int CoachUserId { get; set; }
        
        [Required]
        [StringLength(100)]
        [Display(Name = "Schedule Name")]
        public string Name { get; set; }
        
        [StringLength(500)]
        [Display(Name = "Description")]
        public string Description { get; set; }
        
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }
        
        [Display(Name = "End Date")]
        public DateTime? EndDate { get; set; }
        
        // For one-time workouts - specific date/time
        [Display(Name = "Scheduled Date")]
        public DateTime? ScheduledDateTime { get; set; }
        
        private bool _isRecurring;
        
        // For recurring workouts
        [Display(Name = "Is Recurring")]
        public bool IsRecurring 
        { 
            get => _isRecurring || (!string.IsNullOrEmpty(RecurrencePattern) && RecurrencePattern != "Once");
            set => _isRecurring = value; 
        }
        
        private string _recurrencePattern;
        
        [StringLength(50)]
        [Display(Name = "Recurrence Pattern")]
        public string RecurrencePattern 
        { 
            get => _recurrencePattern; 
            set 
            { 
                _recurrencePattern = value;
                if (!string.IsNullOrEmpty(value) && value != "Once")
                {
                    _isRecurring = true;
                }
            } 
        }
        
        [Display(Name = "Recurrence Day Of Week")]
        public int? RecurrenceDayOfWeek { get; set; } // Store as int (0 = Sunday, 1 = Monday, etc.)
        
        [StringLength(100)]
        [Display(Name = "Multiple Days Of Week")]
        public string MultipleDaysOfWeek { get; set; } // Store as comma-separated integers
        
        [Display(Name = "Recurrence Day Of Month")]
        public int? RecurrenceDayOfMonth { get; set; }
        
        // Notification settings
        [Display(Name = "Send Reminder")]
        public bool SendReminder { get; set; } = true;
        
        [Display(Name = "Reminder Hours Before")]
        public int ReminderHoursBefore { get; set; } = 24;
        
        [Display(Name = "Last Reminder Sent")]
        public DateTime? LastReminderSent { get; set; }
        
        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        [ForeignKey("TemplateAssignmentId")]
        public TemplateAssignment TemplateAssignment { get; set; }
        
        [ForeignKey("TemplateId")]
        public WorkoutTemplate Template { get; set; }
        
        [ForeignKey("ClientUserId")]
        public User Client { get; set; }
        
        [ForeignKey("CoachUserId")]
        public User Coach { get; set; }
        
        /// <summary>
        /// Ensures that IsRecurring is always consistent with the RecurrencePattern
        /// </summary>
        public void EnsureConsistentRecurringState()
        {
            // If RecurrencePattern is not "Once" and not empty, ensure IsRecurring is true in the database
            if (!string.IsNullOrEmpty(RecurrencePattern) && RecurrencePattern != "Once")
            {
                _isRecurring = true;
            }
            else
            {
                // Otherwise, make sure it's set to false
                _isRecurring = false;
            }
        }
    }
}