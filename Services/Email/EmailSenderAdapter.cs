using Microsoft.AspNetCore.Identity.UI.Services;
using System.Threading.Tasks;

namespace WorkoutTrackerWeb.Services.Email
{
    // This adapter class makes our IEmailService compatible with Identity's IEmailSender
    public class EmailSenderAdapter : IEmailSender
    {
        private readonly IEmailService _emailService;

        public EmailSenderAdapter(IEmailService emailService)
        {
            _emailService = emailService;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            await _emailService.SendEmailAsync(email, subject, htmlMessage);
        }
    }
}