using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using WorkoutTrackerweb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Services;
using WorkoutTrackerWeb.Services.Email;

namespace WorkoutTrackerWeb.Pages.Feedback
{
    public class CreateModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly UserService _userService;
        private readonly IEmailService _emailService;
        private readonly ILogger<CreateModel> _logger;
        private readonly UserManager<IdentityUser> _userManager;

        public CreateModel(WorkoutTrackerWebContext context, 
                          UserService userService,
                          IEmailService emailService,
                          ILogger<CreateModel> logger,
                          UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userService = userService;
            _emailService = emailService;
            _logger = logger;
            _userManager = userManager;
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
            if (!ModelState.IsValid)
            {
                PopulateFeedbackTypeOptions();
                return Page();
            }

            // Get current user if logged in
            var currentUser = await _userService.GetOrCreateCurrentUserAsync();
            
            // Create new feedback entry
            var feedbackEntry = new Models.Feedback
            {
                Subject = FeedbackInput.Subject,
                Message = FeedbackInput.Message,
                ContactEmail = FeedbackInput.ContactEmail,
                Type = (FeedbackType)FeedbackInput.Type,
                SubmissionDate = System.DateTime.Now,
                Status = FeedbackStatus.New,
                UserId = currentUser?.UserId
            };
            
            // Save to database
            _context.Feedback.Add(feedbackEntry);
            await _context.SaveChangesAsync();
            
            // Send email notification
            var emailSent = await _emailService.SendFeedbackEmailAsync(
                FeedbackInput.Subject, 
                FeedbackInput.Message,
                FeedbackInput.ContactEmail);
                
            // Log the submission
            _logger.LogInformation(
                "Feedback submitted. ID: {FeedbackId}, Type: {FeedbackType}, Email sent: {EmailSent}",
                feedbackEntry.FeedbackId,
                feedbackEntry.Type,
                emailSent);
            
            // Set success message
            IsSuccess = true;
            StatusMessage = "Your feedback has been submitted. Thank you for helping us improve!";
            
            // Redirect to avoid resubmission
            return RedirectToPage();
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