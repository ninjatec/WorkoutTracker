using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Areas.Coach.Pages.ErrorHandling;
using WorkoutTrackerWeb.Models.Coaching;

namespace WorkoutTrackerWeb.Services.Validation
{
    /// <summary>
    /// Provides standardized validation for coaching client features
    /// </summary>
    public class CoachingValidationService
    {
        private readonly ILogger<CoachingValidationService> _logger;

        public CoachingValidationService(ILogger<CoachingValidationService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Validates a coaching model and returns validation results
        /// </summary>
        /// <typeparam name="T">The model type to validate</typeparam>
        /// <param name="model">The model instance to validate</param>
        /// <returns>A tuple containing validation status and error messages</returns>
        public (bool IsValid, IEnumerable<string> ErrorMessages) ValidateModel<T>(T model) where T : class
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(model);
            var isValid = Validator.TryValidateObject(model, validationContext, validationResults, true);

            return (isValid, validationResults.Select(r => r.ErrorMessage));
        }

        /// <summary>
        /// Validates a relationship between coach and client
        /// </summary>
        /// <param name="relationship">The relationship to validate</param>
        /// <returns>A tuple containing validation status and error message</returns>
        public (bool IsValid, string ErrorMessage) ValidateCoachClientRelationship(CoachClientRelationship relationship)
        {
            if (relationship == null)
            {
                return (false, "Client relationship not found or you don't have permission to access it.");
            }

            if (relationship.Status != RelationshipStatus.Active)
            {
                return (false, $"This client relationship is not active (current status: {relationship.Status}).");
            }

            return (true, null);
        }

        /// <summary>
        /// Validates a client group
        /// </summary>
        /// <param name="group">The client group to validate</param>
        /// <param name="coachId">The ID of the coach</param>
        /// <returns>A tuple containing validation status and error message</returns>
        public (bool IsValid, string ErrorMessage) ValidateClientGroup(ClientGroup group, string coachId)
        {
            if (group == null)
            {
                return (false, "Group not found or you don't have access to it.");
            }

            if (group.CoachId != coachId)
            {
                return (false, "You don't have permission to access this group.");
            }

            return (true, null);
        }

        /// <summary>
        /// Handles model state validation errors in a standardized way
        /// </summary>
        /// <param name="page">The page model</param>
        /// <param name="modelState">The model state</param>
        /// <returns>True if model is valid, false otherwise</returns>
        public bool HandleModelStateValidation(PageModel page, ModelStateDictionary modelState)
        {
            if (modelState.IsValid)
            {
                return true;
            }

            var errors = string.Join("; ", modelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));

            ErrorUtils.HandleValidationError(page, "Please correct the following errors: " + errors);
            return false;
        }

        /// <summary>
        /// Validates required input parameters
        /// </summary>
        /// <param name="page">The page model for error handling</param>
        /// <param name="validations">Dictionary of field names and validation functions</param>
        /// <returns>True if all validations pass, false otherwise</returns>
        public bool ValidateRequiredInputs(PageModel page, Dictionary<string, Func<bool>> validations)
        {
            foreach (var validation in validations)
            {
                if (!validation.Value())
                {
                    ErrorUtils.HandleValidationError(page, $"{validation.Key} is required or invalid.");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Validates a workout template assignment
        /// </summary>
        /// <param name="templateId">The template ID</param>
        /// <param name="assignmentName">The assignment name</param>
        /// <param name="page">The page model for error handling</param>
        /// <returns>True if valid, false otherwise</returns>
        public bool ValidateTemplateAssignment(int templateId, string assignmentName, PageModel page)
        {
            if (templateId <= 0)
            {
                ErrorUtils.HandleValidationError(page, "A valid template must be selected.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(assignmentName))
            {
                ErrorUtils.HandleValidationError(page, "Assignment name is required.");
                return false;
            }

            if (assignmentName.Length > 100)
            {
                ErrorUtils.HandleValidationError(page, "Assignment name cannot exceed 100 characters.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates client group creation/update parameters
        /// </summary>
        /// <param name="groupName">The group name</param>
        /// <param name="groupDescription">The group description</param>
        /// <param name="page">The page model for error handling</param>
        /// <returns>True if valid, false otherwise</returns>
        public bool ValidateClientGroup(string groupName, string groupDescription, PageModel page)
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                ErrorUtils.HandleValidationError(page, "Group name is required.");
                return false;
            }

            if (groupName.Length > 100)
            {
                ErrorUtils.HandleValidationError(page, "Group name cannot exceed 100 characters.");
                return false;
            }

            if (groupDescription?.Length > 500)
            {
                ErrorUtils.HandleValidationError(page, "Group description cannot exceed 500 characters.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Handles errors when ModelState is invalid
        /// </summary>
        /// <param name="page">The Razor Page</param>
        public void HandleInvalidModelState(PageModel page)
        {
            var errors = string.Join("; ", page.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));
            
            SetError(page, "Please correct the following errors: " + errors);
        }

        /// <summary>
        /// Validates a group name
        /// </summary>
        /// <param name="groupName">The group name to validate</param>
        /// <param name="page">The Razor Page</param>
        /// <returns>True if valid, False if invalid</returns>
        public bool ValidateGroupName(string groupName, PageModel page)
        {
            if (string.IsNullOrEmpty(groupName))
            {
                SetError(page, "Group name is required.");
                return false;
            }

            if (groupName.Length > 100)
            {
                SetError(page, "Group name cannot exceed 100 characters.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates a group description
        /// </summary>
        /// <param name="groupDescription">The group description to validate</param>
        /// <param name="page">The Razor Page</param>
        /// <returns>True if valid, False if invalid</returns>
        public bool ValidateGroupDescription(string groupDescription, PageModel page)
        {
            if (groupDescription?.Length > 500)
            {
                SetError(page, "Group description cannot exceed 500 characters.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates client selection for group operations
        /// </summary>
        /// <param name="selectedClients">The list of selected client IDs</param>
        /// <param name="page">The Razor Page</param>
        /// <returns>True if valid, False if invalid</returns>
        public bool ValidateClientSelection(List<int> selectedClients, PageModel page)
        {
            if (selectedClients == null || !selectedClients.Any())
            {
                SetError(page, "No clients selected.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates that a required field is not empty
        /// </summary>
        /// <param name="value">The value to check</param>
        /// <param name="fieldName">The name of the field for error messages</param>
        /// <param name="page">The Razor Page</param>
        /// <returns>True if valid, False if invalid</returns>
        public bool ValidateRequiredField(string value, string fieldName, PageModel page)
        {
            if (string.IsNullOrEmpty(value))
            {
                SetError(page, $"The {fieldName} is required.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates a template name
        /// </summary>
        /// <param name="templateName">The template name to validate</param>
        /// <param name="page">The Razor Page</param>
        /// <returns>True if valid, False if invalid</returns>
        public bool ValidateTemplateName(string templateName, PageModel page)
        {
            if (string.IsNullOrEmpty(templateName))
            {
                SetError(page, "Template name is required.");
                return false;
            }

            if (templateName.Length > 100)
            {
                SetError(page, "Template name cannot exceed 100 characters.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates an email address format
        /// </summary>
        /// <param name="email">The email to validate</param>
        /// <param name="page">The Razor Page</param>
        /// <returns>True if valid, False if invalid</returns>
        public bool ValidateEmailFormat(string email, PageModel page)
        {
            if (string.IsNullOrEmpty(email))
            {
                SetError(page, "Email address is required.");
                return false;
            }

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                SetError(page, "Invalid email format.");
                return false;
            }
        }

        /// <summary>
        /// Validates an invitation message
        /// </summary>
        /// <param name="message">The invitation message to validate</param>
        /// <param name="page">The Razor Page</param>
        /// <returns>True if valid, False if invalid</returns>
        public bool ValidateInvitationMessage(string message, PageModel page)
        {
            if (message?.Length > 2000)
            {
                SetError(page, "Invitation message cannot exceed 2000 characters.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates a goal description
        /// </summary>
        /// <param name="description">The goal description to validate</param>
        /// <param name="page">The Razor Page</param>
        /// <returns>True if valid, False if invalid</returns>
        public bool ValidateGoalDescription(string description, PageModel page)
        {
            if (string.IsNullOrEmpty(description))
            {
                SetError(page, "Goal description is required.");
                return false;
            }

            if (description.Length > 255)
            {
                SetError(page, "Goal description cannot exceed 255 characters.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Sets an error message on the page
        /// </summary>
        /// <param name="page">The Razor Page</param>
        /// <param name="message">The error message</param>
        public void SetError(PageModel page, string message)
        {
            ErrorUtils.HandleValidationError(page, message);
        }

        /// <summary>
        /// Sets a success message on the page
        /// </summary>
        /// <param name="page">The Razor Page</param>
        /// <param name="message">The success message</param>
        public void SetSuccess(PageModel page, string message)
        {
            ErrorUtils.SetSuccessMessage(page, message);
        }

        /// <summary>
        /// Sets an informational message on the page
        /// </summary>
        /// <param name="page">The Razor Page</param>
        /// <param name="message">The informational message</param>
        public void SetInfo(PageModel page, string message)
        {
            ErrorUtils.SetInfoMessage(page, message);
        }

        /// <summary>
        /// Handles exceptions using a standardized approach
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="ex">The exception</param>
        /// <param name="page">The Razor Page</param>
        /// <param name="userMessage">The message to show to the user</param>
        /// <param name="context">Context information for logging</param>
        public void HandleException(ILogger logger, Exception ex, PageModel page, string userMessage, string context)
        {
            ErrorUtils.HandleException(logger, ex, page, userMessage, context);
        }
    }
}