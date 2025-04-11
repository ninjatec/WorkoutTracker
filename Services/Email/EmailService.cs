using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Options;

namespace WorkoutTrackerWeb.Services.Email
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
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
                // Log the error - in a production app, you'd use a proper logging framework
                Console.WriteLine($"Error sending email: {ex.Message}");
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
    }
}