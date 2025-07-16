using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Services;
using WorkoutTrackerWeb.Services.Email;
using WorkoutTrackerWeb.Services.Logging;

namespace WorkoutTrackerWeb.Pages.Feedback
{
    public class CreateModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly UserService _userService;
        private readonly IEmailService _emailService;
        private readonly ILogger<CreateModel> _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ISecureErrorLoggingService _secureErrorLogging;

        public CreateModel(WorkoutTrackerWebContext context, 
                          UserService userService,
                          IEmailService emailService,
                          ILogger<CreateModel> logger,
                          UserManager<IdentityUser> userManager,
                          ISecureErrorLoggingService secureErrorLogging)
        {
            _context = context;
            _userService = userService;
            _emailService = emailService;
            _logger = logger;
            _userManager = userManager;
            _secureErrorLogging = secureErrorLogging;
        }

        [TempData]
        public string StatusMessage { get; set; }
        
        [TempData]
        public bool IsSuccess { get; set; }

        [BindProperty]
        public FeedbackInputModel FeedbackInput { get; set; } = new FeedbackInputModel();
        
        public SelectList FeedbackTypeOptions { get; set; }

        public async Task OnGetAsync()
        {
            PopulateFeedbackTypeOptions();
            
            // Pre-fill email if user is logged in
            var identityUserId = _userService.GetCurrentIdentityUserId();
            
            if (!string.IsNullOrEmpty(identityUserId))
            {
                var identityUser = await _userManager.FindByIdAsync(identityUserId);
                
                if (identityUser != null)
                {
                    FeedbackInput.ContactEmail = identityUser.Email;
                }
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    PopulateFeedbackTypeOptions();
                    return Page();
                }

                // Validate input to prevent potential security issues
                if (string.IsNullOrWhiteSpace(FeedbackInput.Subject) || 
                    string.IsNullOrWhiteSpace(FeedbackInput.Message))
                {
                    ModelState.AddModelError("", "Subject and message are required.");
                    PopulateFeedbackTypeOptions();
                    return Page();
                }

                // Get current user if logged in
                var currentUser = await _userService.GetOrCreateCurrentUserAsync();
                
                // Create new feedback entry with additional validation
                var feedbackEntry = new Models.Feedback
                {
                    Subject = FeedbackInput.Subject.Trim(),
                    Message = FeedbackInput.Message.Trim(),
                    ContactEmail = string.IsNullOrWhiteSpace(FeedbackInput.ContactEmail) ? null : FeedbackInput.ContactEmail.Trim(),
                    Type = (FeedbackType)FeedbackInput.Type,
                    SubmissionDate = DateTime.UtcNow,
                    Status = FeedbackStatus.New,
                    UserId = currentUser?.UserId
                };
                
                // Save to database with error handling
                _context.Feedback.Add(feedbackEntry);
                await _context.SaveChangesAsync();
                
                // Attempt to send email notification - don't fail if email fails
                bool emailSent = false;
                try
                {
                    emailSent = await _emailService.SendFeedbackEmailAsync(
                        FeedbackInput.Subject, 
                        FeedbackInput.Message,
                        FeedbackInput.ContactEmail);
                }
                catch (Exception emailEx)
                {
                    // Log email failure but don't expose to user
                    _logger.LogWarning(emailEx, "Failed to send feedback email notification for feedback ID {FeedbackId}", 
                        feedbackEntry.FeedbackId);
                }
                    
                // Log the submission
                _logger.LogInformation(
                    "Feedback submitted successfully. ID: {FeedbackId}, Type: {FeedbackType}, Email sent: {EmailSent}",
                    feedbackEntry.FeedbackId,
                    feedbackEntry.Type,
                    emailSent);
                
                // Set success message
                IsSuccess = true;
                StatusMessage = "Your feedback has been submitted successfully. Thank you for helping us improve!";
                
                // Redirect to avoid resubmission
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                // Use secure error logging service for comprehensive security monitoring
                await _secureErrorLogging.LogSecurityExceptionAsync(ex, HttpContext, "Feedback submission failed");
                
                // Log the error with secure logging
                _logger.LogError(ex, "Error occurred while submitting feedback. User: {User}", 
                    HttpContext.User?.Identity?.Name ?? "Anonymous");
                
                // Show user-friendly error message without exposing sensitive details
                ModelState.AddModelError("", "We're sorry, but there was an error submitting your feedback. Please try again later.");
                
                IsSuccess = false;
                StatusMessage = "An error occurred while submitting your feedback. Please try again.";
                
                PopulateFeedbackTypeOptions();
                return Page();
            }
        }

        private void PopulateFeedbackTypeOptions()
        {
            FeedbackTypeOptions = new SelectList(
                new[]
                {
                    new { Value = (int)FeedbackType.BugReport, Text = "Bug Report" },
                    new { Value = (int)FeedbackType.FeatureRequest, Text = "Feature Request" },
                    new { Value = (int)FeedbackType.GeneralFeedback, Text = "General Feedback" },
                    new { Value = (int)FeedbackType.Question, Text = "Question" }
                },
                "Value", "Text");
        }

        public class FeedbackInputModel
        {
            public int Type { get; set; }
            
            [Required]
            [StringLength(100, ErrorMessage = "Subject can't be longer than 100 characters.")]
            [Display(Name = "Subject")]
            public string Subject { get; set; }
            
            [Required]
            [StringLength(5000, ErrorMessage = "Message can't be longer than 5000 characters.")]
            [Display(Name = "Message")]
            public string Message { get; set; }
            
            [EmailAddress]
            [Display(Name = "Your Email (optional)")]
            public string ContactEmail { get; set; }
        }
    }
}