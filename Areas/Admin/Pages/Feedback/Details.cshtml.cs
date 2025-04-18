using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerweb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Services.Email;

namespace WorkoutTrackerWeb.Areas.Admin.Pages.Feedback
{
    [Authorize(Roles = "Admin")]
    public class DetailsModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(
            WorkoutTrackerWebContext context,
            UserManager<IdentityUser> userManager,
            IEmailService emailService,
            ILogger<DetailsModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _logger = logger;
        }

        [BindProperty]
        public Models.Feedback Feedback { get; set; }
        
        public List<IdentityUser> AdminUsers { get; set; }
        public string AssignedToAdminName { get; set; }
        
        [TempData]
        public string StatusMessage { get; set; }
        
        [TempData]
        public bool IsSuccess { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Get feedback with user data
            Feedback = await _context.Feedback
                .Include(f => f.User)
                .FirstOrDefaultAsync(m => m.FeedbackId == id);

            if (Feedback == null)
            {
                return NotFound();
            }

            // Load admin users for assignment dropdown
            await LoadAdminUsersAsync();
            
            // Get assigned admin name if assigned
            if (!string.IsNullOrEmpty(Feedback.AssignedToAdminId))
            {
                var assignedAdmin = await _userManager.FindByIdAsync(Feedback.AssignedToAdminId);
                AssignedToAdminName = assignedAdmin?.UserName ?? "Unknown";
            }
            
            return Page();
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync(int feedbackId, int newStatus)
        {
            var feedback = await _context.Feedback.FindAsync(feedbackId);
            
            if (feedback == null)
            {
                return NotFound();
            }

            if (!Enum.IsDefined(typeof(FeedbackStatus), newStatus))
            {
                IsSuccess = false;
                StatusMessage = "Invalid status value.";
                return RedirectToPage(new { id = feedbackId });
            }
            
            // Record old status for change tracking
            var oldStatus = feedback.Status;
            var statusChanged = oldStatus != (FeedbackStatus)newStatus;
            
            // Update status and last updated timestamp
            feedback.Status = (FeedbackStatus)newStatus;
            feedback.LastUpdated = DateTime.Now;
            
            // Add status change to admin notes
            if (statusChanged)
            {
                string timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                string statusNote = $"[{timestamp}] Status changed from {oldStatus} to {feedback.Status}.\n";
                
                if (string.IsNullOrEmpty(feedback.AdminNotes))
                {
                    feedback.AdminNotes = statusNote;
                }
                else
                {
                    feedback.AdminNotes = statusNote + "\n" + feedback.AdminNotes;
                }
                
                // If contact email exists, send notification about status change
                if (!string.IsNullOrEmpty(feedback.ContactEmail))
                {
                    await SendStatusUpdateEmailAsync(feedback, oldStatus);
                }
            }
            
            await _context.SaveChangesAsync();
            
            IsSuccess = true;
            StatusMessage = $"Feedback status updated to {feedback.Status}.";
            
            return RedirectToPage(new { id = feedbackId });
        }
        
        public async Task<IActionResult> OnPostUpdatePriorityAsync(int feedbackId, string newPriority)
        {
            var feedback = await _context.Feedback.FindAsync(feedbackId);
            
            if (feedback == null)
            {
                return NotFound();
            }
            
            // Update priority
            if (string.IsNullOrEmpty(newPriority))
            {
                feedback.Priority = null;
                StatusMessage = "Priority cleared.";
            }
            else if (Enum.TryParse<FeedbackPriority>(newPriority, out var priority))
            {
                feedback.Priority = priority;
                StatusMessage = $"Priority set to {priority}.";
            }
            else
            {
                StatusMessage = "Invalid priority value.";
                IsSuccess = false;
                return RedirectToPage(new { id = feedbackId });
            }
            
            feedback.LastUpdated = DateTime.Now;
            await _context.SaveChangesAsync();
            
            IsSuccess = true;
            
            return RedirectToPage(new { id = feedbackId });
        }
        
        public async Task<IActionResult> OnPostTogglePublishAsync(int id, bool isPublished)
        {
            var feedback = await _context.Feedback.FindAsync(id);
            
            if (feedback == null)
            {
                return NotFound();
            }
            
            // Check if there is a public response for publishing
            if (isPublished && string.IsNullOrEmpty(feedback.PublicResponse))
            {
                IsSuccess = false;
                StatusMessage = "Cannot publish feedback without a public response. Please add a response first.";
                return RedirectToPage(new { id = feedback.FeedbackId });
            }
            
            feedback.IsPublished = isPublished;
            feedback.LastUpdated = DateTime.Now;
            
            await _context.SaveChangesAsync();
            
            IsSuccess = true;
            StatusMessage = isPublished ? "Feedback published successfully." : "Feedback unpublished.";
            
            return RedirectToPage(new { id = feedback.FeedbackId });
        }
        
        public async Task<IActionResult> OnPostReplyAsync(int id, string publicResponse, string adminNotes, DateTime? estimatedCompletionDate, bool sendEmail)
        {
            var feedback = await _context.Feedback.FindAsync(id);
            
            if (feedback == null)
            {
                return NotFound();
            }
            
            // Update feedback with reply information
            feedback.PublicResponse = publicResponse;
            
            if (!string.IsNullOrEmpty(adminNotes))
            {
                string timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                string newNote = $"[{timestamp}] {adminNotes}";
                
                if (string.IsNullOrEmpty(feedback.AdminNotes))
                {
                    feedback.AdminNotes = newNote;
                }
                else
                {
                    feedback.AdminNotes = newNote + "\n\n" + feedback.AdminNotes;
                }
            }
            
            feedback.EstimatedCompletionDate = estimatedCompletionDate;
            feedback.LastUpdated = DateTime.Now;
            
            // If providing a response, set status to in progress if currently new
            if (feedback.Status == FeedbackStatus.New && !string.IsNullOrEmpty(publicResponse))
            {
                feedback.Status = FeedbackStatus.InProgress;
            }
            
            await _context.SaveChangesAsync();
            
            // Send email if requested and contact email exists
            if (sendEmail && !string.IsNullOrEmpty(feedback.ContactEmail))
            {
                await SendReplyEmailAsync(feedback, publicResponse);
                StatusMessage = "Reply sent and feedback updated.";
            }
            else
            {
                StatusMessage = "Feedback updated successfully.";
            }
            
            IsSuccess = true;
            
            return RedirectToPage(new { id = feedback.FeedbackId });
        }
        
        public async Task<IActionResult> OnPostAssignAsync(int id, string adminUserId)
        {
            var feedback = await _context.Feedback.FindAsync(id);
            
            if (feedback == null)
            {
                return NotFound();
            }
            
            // Update assignment
            if (string.IsNullOrEmpty(adminUserId))
            {
                feedback.AssignedToAdminId = null;
                StatusMessage = "Feedback unassigned.";
            }
            else
            {
                // Verify admin user exists
                var admin = await _userManager.FindByIdAsync(adminUserId);
                if (admin == null || !await _userManager.IsInRoleAsync(admin, "Admin"))
                {
                    IsSuccess = false;
                    StatusMessage = "Invalid admin user selected.";
                    return RedirectToPage(new { id = feedback.FeedbackId });
                }
                
                feedback.AssignedToAdminId = adminUserId;
                StatusMessage = $"Feedback assigned to {admin.UserName}.";
            }
            
            feedback.LastUpdated = DateTime.Now;
            await _context.SaveChangesAsync();
            
            IsSuccess = true;
            
            return RedirectToPage(new { id = feedback.FeedbackId });
        }

        private async Task LoadAdminUsersAsync()
        {
            // Get list of users in Admin role
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            AdminUsers = admins.ToList();
        }
        
        private async Task SendStatusUpdateEmailAsync(Models.Feedback feedback, FeedbackStatus oldStatus)
        {
            var subject = $"Update on your feedback: {feedback.Subject}";
            var message = $"Your feedback has been updated.\n\n" +
                         $"Status changed from: {oldStatus} to: {feedback.Status}\n\n" +
                         $"Subject: {feedback.Subject}\n\n";
            
            if (feedback.Status == FeedbackStatus.Completed && !string.IsNullOrEmpty(feedback.PublicResponse))
            {
                message += $"Response: {feedback.PublicResponse}\n\n";
            }
            
            message += "Thank you for helping us improve Workout Tracker!";
            
            try
            {
                await _emailService.SendFeedbackEmailAsync(subject, message, feedback.ContactEmail);
                _logger.LogInformation("Status update email sent to {Email} for feedback ID {FeedbackId}", 
                    feedback.ContactEmail, feedback.FeedbackId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending status update email for feedback ID {FeedbackId}", feedback.FeedbackId);
            }
        }
        
        private async Task SendReplyEmailAsync(Models.Feedback feedback, string response)
        {
            var subject = $"Response to your feedback: {feedback.Subject}";
            var message = $"Thank you for your feedback.\n\n" +
                         $"Subject: {feedback.Subject}\n\n" +
                         $"Our response:\n{response}\n\n";
                         
            if (feedback.Status == FeedbackStatus.InProgress)
            {
                message += "We are currently working on addressing your feedback.\n\n";
                
                if (feedback.EstimatedCompletionDate.HasValue)
                {
                    message += $"Estimated completion: {feedback.EstimatedCompletionDate.Value.ToString("dd/MM/yyyy")}\n\n";
                }
            }
            else if (feedback.Status == FeedbackStatus.Completed)
            {
                message += "Your feedback has been fully addressed.\n\n";
            }
            
            message += "Thank you for helping us improve Workout Tracker!";
            
            try
            {
                await _emailService.SendFeedbackEmailAsync(subject, message, feedback.ContactEmail);
                _logger.LogInformation("Reply email sent to {Email} for feedback ID {FeedbackId}", 
                    feedback.ContactEmail, feedback.FeedbackId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending reply email for feedback ID {FeedbackId}", feedback.FeedbackId);
            }
        }
    }
}