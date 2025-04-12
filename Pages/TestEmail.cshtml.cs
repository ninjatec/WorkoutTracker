using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using WorkoutTrackerWeb.Services.Email;

namespace WorkoutTrackerWeb.Pages
{
    public class TestEmailModel : PageModel
    {
        private readonly IEmailService _emailService;
        private readonly EmailSettings _emailSettings;

        public TestEmailModel(IEmailService emailService, IOptions<EmailSettings> emailSettings)
        {
            _emailService = emailService;
            _emailSettings = emailSettings.Value;
        }

        [TempData]
        public string StatusMessage { get; set; }

        [TempData]
        public bool IsSuccess { get; set; }

        public EmailSettings Settings => _emailSettings;

        [BindProperty]
        public string RecipientEmail { get; set; }

        public void OnGet()
        {
            // Page load logic
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrEmpty(RecipientEmail))
            {
                StatusMessage = "Error: Please provide a valid email address.";
                IsSuccess = false;
                return Page();
            }

            try
            {
                string subject = "WorkoutTracker - Email Test";
                string message = @"
                    <h1>Email Test Successful!</h1>
                    <p>This email confirms that your WorkoutTracker email configuration is working correctly.</p>
                    <p>You can now use the email functionality for user registration and other features.</p>
                    <p>Email settings used:</p>
                    <ul>
                        <li><strong>Mail Server:</strong> " + _emailSettings.MailServer + @"</li>
                        <li><strong>Port:</strong> " + _emailSettings.MailPort + @"</li>
                        <li><strong>Sender:</strong> " + _emailSettings.SenderName + @" (" + _emailSettings.SenderEmail + @")</li>
                    </ul>
                ";

                await _emailService.SendEmailAsync(RecipientEmail, subject, message);
                
                StatusMessage = "Test email sent successfully to " + RecipientEmail;
                IsSuccess = true;
            }
            catch (System.Exception ex)
            {
                StatusMessage = $"Error sending email: {ex.Message}";
                IsSuccess = false;
            }

            return Page();
        }
    }
}