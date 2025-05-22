using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace WorkoutTrackerWeb.Services.Email
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger = null)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            try
            {
                var mimeMessage = new MimeMessage();
                
                mimeMessage.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
                mimeMessage.To.Add(MailboxAddress.Parse(email));
                mimeMessage.Subject = subject;
                
                var builder = new BodyBuilder
                {
                    HtmlBody = message
                };
                
                mimeMessage.Body = builder.ToMessageBody();
                
                using var client = new SmtpClient();
                
                // Use StartTls for port 587 as required by Office 365
                SecureSocketOptions secureOption = _emailSettings.MailPort == 587 
                    ? SecureSocketOptions.StartTls 
                    : _emailSettings.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.None;
                
                await client.ConnectAsync(_emailSettings.MailServer, _emailSettings.MailPort, secureOption);
                
                if (!string.IsNullOrEmpty(_emailSettings.UserName))
                {
                    await client.AuthenticateAsync(_emailSettings.UserName, _emailSettings.Password);
                }
                
                await client.SendAsync(mimeMessage);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error sending email: {ex.Message}");
                throw;
            }
        }

        public async Task SendEmailConfirmationAsync(string email, string callbackUrl)
        {
            string subject = "Confirm your email for WorkoutTracker";
            string message = $@"
                <h1>Welcome to WorkoutTracker!</h1>
                <p>Please confirm your email address by clicking the link below:</p>
                <p><a href='{callbackUrl}'>Confirm Email</a></p>
                <p>If you didn't create this account, you can ignore this email.</p>";
                
            await SendEmailAsync(email, subject, message);
        }

        public async Task SendPasswordResetAsync(string email, string callbackUrl)
        {
            string subject = "Reset your WorkoutTracker password";
            string message = $@"
                <h1>Password Reset</h1>
                <p>You requested a password reset for your WorkoutTracker account.</p>
                <p>Please click the link below to reset your password:</p>
                <p><a href='{callbackUrl}'>Reset Password</a></p>
                <p>If you didn't request a password reset, you can ignore this email.</p>";
                
            await SendEmailAsync(email, subject, message);
        }
        
        public async Task<bool> SendFeedbackEmailAsync(string subject, string message, string senderEmail = null)
        {
            try
            {
                _logger?.LogInformation($"Attempting to send feedback email using server: {_emailSettings.MailServer}:{_emailSettings.MailPort}");
                
                var adminEmail = _emailSettings.AdminEmail ?? "marc.coxall@ninjatec.co.uk";
                
                var mimeMessage = new MimeMessage();
                mimeMessage.From.Add(new MailboxAddress($"{_emailSettings.SenderName} Feedback", _emailSettings.SenderEmail));
                mimeMessage.To.Add(MailboxAddress.Parse(adminEmail));
                mimeMessage.Subject = $"Workout Tracker Feedback: {subject}";
                
                // If reply is expected, set the reply-to header
                if (!string.IsNullOrEmpty(senderEmail))
                {
                    mimeMessage.ReplyTo.Add(MailboxAddress.Parse(senderEmail));
                }
                
                var builder = new BodyBuilder
                {
                    HtmlBody = $"<h3>New Feedback Submission</h3>" +
                               $"<p><strong>Subject:</strong> {subject}</p>" +
                               $"<p><strong>Message:</strong></p>" +
                               $"<p>{message.Replace(Environment.NewLine, "<br>")}</p>" +
                               (string.IsNullOrEmpty(senderEmail) ? "" : $"<p><strong>Contact Email:</strong> {senderEmail}</p>")
                };
                
                mimeMessage.Body = builder.ToMessageBody();
                
                using var client = new SmtpClient();
                
                SecureSocketOptions secureOption = _emailSettings.MailPort == 587
                    ? SecureSocketOptions.StartTls
                    : _emailSettings.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.None;
                
                await client.ConnectAsync(_emailSettings.MailServer, _emailSettings.MailPort, secureOption);
                
                if (!string.IsNullOrEmpty(_emailSettings.UserName))
                {
                    await client.AuthenticateAsync(_emailSettings.UserName, _emailSettings.Password);
                }
                
                await client.SendAsync(mimeMessage);
                await client.DisconnectAsync(true);
                
                _logger?.LogInformation($"Successfully sent feedback email with subject: {subject}");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Failed to send feedback email: {ex.Message}");
                return false;
            }
        }
    }
}