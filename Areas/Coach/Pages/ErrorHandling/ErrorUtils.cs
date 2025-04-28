using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;

namespace WorkoutTrackerWeb.Areas.Coach.Pages.ErrorHandling
{
    /// <summary>
    /// Utility class for standardized error handling across coach pages
    /// </summary>
    public static class ErrorUtils
    {
        /// <summary>
        /// Handles exceptions in a standardized way for all coach pages
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="ex">The exception that was caught</param>
        /// <param name="page">The PageModel instance</param>
        /// <param name="userFriendlyMessage">Optional user-friendly message to display</param>
        /// <param name="logContext">Additional context for the log entry</param>
        /// <returns>True if the error was handled successfully</returns>
        public static bool HandleException(
            ILogger logger, 
            Exception ex, 
            PageModel page,
            string userFriendlyMessage = null,
            string logContext = null)
        {
            if (logger == null || page == null)
                return false;
                
            // Set a descriptive but safe error message
            string errorMessage = string.IsNullOrEmpty(userFriendlyMessage)
                ? "An error occurred while processing your request. Please try again."
                : userFriendlyMessage;
                
            // Add exception message in development environment only
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                errorMessage += $" Error details: {ex.Message}";
            }
            
            // Log the error with appropriate level and context
            logger.LogError(ex, "Error in Coach area{Context}: {Message}", 
                string.IsNullOrEmpty(logContext) ? "" : $" - {logContext}", ex.Message);
            
            // Set the error message in TempData
            page.TempData["ErrorMessage"] = errorMessage;
            
            return true;
        }

        /// <summary>
        /// Handles validation errors in a standardized way
        /// </summary>
        /// <param name="page">The PageModel instance</param>
        /// <param name="validationMessage">The validation error message</param>
        /// <returns>True if the error was handled successfully</returns>
        public static bool HandleValidationError(
            PageModel page,
            string validationMessage)
        {
            if (page == null || string.IsNullOrEmpty(validationMessage))
                return false;
                
            // Set the error message in TempData
            page.TempData["ErrorMessage"] = validationMessage;
            
            return true;
        }
        
        /// <summary>
        /// Sets a success message in a standardized way
        /// </summary>
        /// <param name="page">The PageModel instance</param>
        /// <param name="message">The success message</param>
        /// <returns>True if the message was set successfully</returns>
        public static bool SetSuccessMessage(
            PageModel page,
            string message)
        {
            if (page == null || string.IsNullOrEmpty(message))
                return false;
                
            // Set the success message in TempData
            page.TempData["SuccessMessage"] = message;
            
            return true;
        }
        
        /// <summary>
        /// Sets an informational message in a standardized way
        /// </summary>
        /// <param name="page">The PageModel instance</param>
        /// <param name="message">The informational message</param>
        /// <returns>True if the message was set successfully</returns>
        public static bool SetInfoMessage(
            PageModel page,
            string message)
        {
            if (page == null || string.IsNullOrEmpty(message))
                return false;
                
            // Set the info message in TempData
            page.TempData["InfoMessage"] = message;
            
            return true;
        }
    }
}