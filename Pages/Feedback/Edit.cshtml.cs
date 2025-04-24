using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Services;
using WorkoutTrackerWeb.Services.Email;

namespace WorkoutTrackerWeb.Pages.Feedback
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<EditModel> _logger;

        public EditModel(WorkoutTrackerWebContext context, 
                       IEmailService emailService, 
                       ILogger<EditModel> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        [BindProperty]
        public Models.Feedback Feedback { get; set; }
        
        public SelectList FeedbackTypeOptions { get; set; }
        public SelectList FeedbackStatusOptions { get; set; }
        
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

            Feedback = await _context.Feedback
                .Include(f => f.User)
                .FirstOrDefaultAsync(m => m.FeedbackId == id);

            if (Feedback == null)
            {
                return NotFound();
            }
            
            PopulateDropdowns();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                PopulateDropdowns();
                return Page();
            }

            // Get the original feedback to track changes
            var originalFeedback = await _context.Feedback.AsNoTracking()
                .FirstOrDefaultAsync(f => f.FeedbackId == Feedback.FeedbackId);
            
            if (originalFeedback == null)
            {
                return NotFound();
            }

            // Track the old status for change detection
            var statusChanged = originalFeedback.Status != Feedback.Status;
            var oldStatus = originalFeedback.Status;

            // Update the feedback entry
            _context.Attach(Feedback).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                
                // Send email notification if status was changed
                if (statusChanged && !string.IsNullOrEmpty(Feedback.ContactEmail))
                {
                    await SendStatusUpdateEmail(originalFeedback, oldStatus);
                }
                
                _logger.LogInformation("Feedback ID {FeedbackId} updated. Status change: {StatusChanged}", 
                    Feedback.FeedbackId, statusChanged);
                
                IsSuccess = true;
                StatusMessage = "Feedback has been updated successfully.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FeedbackExists(Feedback.FeedbackId))
                {
                    return NotFound();
                }
                else
                {
                    _logger.LogError("Concurrency error while updating feedback ID {FeedbackId}", Feedback.FeedbackId);
                    IsSuccess = false;
                    StatusMessage = "Error: The feedback was modified by another user. Please try again.";
                    throw;
                }
            }

            return RedirectToPage("./Details", new { id = Feedback.FeedbackId });
        }

        private bool FeedbackExists(int id)
        {
            return _context.Feedback.Any(e => e.FeedbackId == id);
        }
        
        private void PopulateDropdowns()
        {
            FeedbackTypeOptions = new SelectList(
                System.Enum.GetValues(typeof(FeedbackType))
                    .Cast<FeedbackType>()
                    .Select(f => new { Value = f, Text = f.ToString() }),
                "Value", "Text");
                
            FeedbackStatusOptions = new SelectList(
                System.Enum.GetValues(typeof(FeedbackStatus))
                    .Cast<FeedbackStatus>()
                    .Select(s => new { Value = s, Text = s.ToString() }),
                "Value", "Text");
        }
        
        private async Task SendStatusUpdateEmail(Models.Feedback originalFeedback, FeedbackStatus oldStatus)
        {
            var subject = $"Update on your feedback: {Feedback.Subject}";
            var message = $"Your feedback has been updated.\n\n" +
                         $"Status changed from: {oldStatus} to: {Feedback.Status}\n\n" +
                         $"Subject: {Feedback.Subject}\n\n";
                         
            if (!string.IsNullOrEmpty(Feedback.AdminNotes))
            {
                message += $"Additional notes:\n{Feedback.AdminNotes}\n\n";
            }
            
            message += "Thank you for helping us improve Workout Tracker!";
            
            await _emailService.SendFeedbackEmailAsync(subject, message, Feedback.ContactEmail);
            
            _logger.LogInformation("Status update email sent to {Email} for feedback ID {FeedbackId}", 
                Feedback.ContactEmail, Feedback.FeedbackId);
        }
    }
}