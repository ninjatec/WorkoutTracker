using System.Threading.Tasks;

namespace WorkoutTrackerWeb.Services.Email
{
    public interface IEmailService
    {
        Task SendEmailAsync(string email, string subject, string message);
        Task SendEmailConfirmationAsync(string email, string callbackUrl);
        Task SendPasswordResetAsync(string email, string callbackUrl);
        Task<bool> SendFeedbackEmailAsync(string subject, string message, string senderEmail = null);
    }
}