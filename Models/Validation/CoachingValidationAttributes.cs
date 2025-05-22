using System;
using System.ComponentModel.DataAnnotations;

namespace WorkoutTrackerWeb.Models.Validation
{
    /// <summary>
    /// Validates a coaching group name
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class GroupNameAttribute : ValidationAttribute
    {
        public GroupNameAttribute()
        {
            ErrorMessage = "Group name is required and cannot exceed 100 characters.";
        }

        public override bool IsValid(object value)
        {
            if (value == null)
                return false;

            string stringValue = value.ToString();
            return !string.IsNullOrWhiteSpace(stringValue) && stringValue.Length <= 100;
        }
    }

    /// <summary>
    /// Validates a coaching group description
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class GroupDescriptionAttribute : ValidationAttribute
    {
        public GroupDescriptionAttribute()
        {
            ErrorMessage = "Group description cannot exceed 500 characters.";
        }

        public override bool IsValid(object value)
        {
            if (value == null)
                return true; // Description is optional

            string stringValue = value.ToString();
            return stringValue.Length <= 500;
        }
    }

    /// <summary>
    /// Validates a template name
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class TemplateNameAttribute : ValidationAttribute
    {
        public TemplateNameAttribute()
        {
            ErrorMessage = "Template name is required and cannot exceed 100 characters.";
        }

        public override bool IsValid(object value)
        {
            if (value == null)
                return false;

            string stringValue = value.ToString();
            return !string.IsNullOrWhiteSpace(stringValue) && stringValue.Length <= 100;
        }
    }

    /// <summary>
    /// Validates an invitation message
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class InvitationMessageAttribute : ValidationAttribute
    {
        public InvitationMessageAttribute()
        {
            ErrorMessage = "Invitation message cannot exceed 2000 characters.";
        }

        public override bool IsValid(object value)
        {
            if (value == null)
                return true; // Message is optional

            string stringValue = value.ToString();
            return stringValue.Length <= 2000;
        }
    }

    /// <summary>
    /// Validates a goal description
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class GoalDescriptionAttribute : ValidationAttribute
    {
        public GoalDescriptionAttribute()
        {
            ErrorMessage = "Goal description is required and cannot exceed 255 characters.";
        }

        public override bool IsValid(object value)
        {
            if (value == null)
                return false;

            string stringValue = value.ToString();
            return !string.IsNullOrWhiteSpace(stringValue) && stringValue.Length <= 255;
        }
    }

    /// <summary>
    /// Validates a coaching note
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class CoachingNoteAttribute : ValidationAttribute
    {
        public CoachingNoteAttribute()
        {
            ErrorMessage = "Note content is required and cannot exceed 2000 characters.";
        }

        public override bool IsValid(object value)
        {
            if (value == null)
                return false;

            string stringValue = value.ToString();
            return !string.IsNullOrWhiteSpace(stringValue) && stringValue.Length <= 2000;
        }
    }
}