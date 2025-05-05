using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WorkoutTrackerWeb.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<bool> SendFeedbackEmailAsync(string subject, string message, string senderEmail = null)
        {
            try
            {
                // Updated to use the same configuration keys as the main email service
                var host = _config["EmailSettings:MailServer"] ?? _config["EmailSettings__MailServer"];
                var port = int.Parse(_config["EmailSettings:MailPort"] ?? _config["EmailSettings__MailPort"] ?? "587");
                var username = _config["EmailSettings:UserName"] ?? _config["EmailSettings__UserName"];
                var password = _config["EmailSettings:Password"] ?? _config["EmailSettings__Password"];
                var enableSsl = bool.Parse(_config["EmailSettings:UseSsl"] ?? _config["EmailSettings__UseSsl"] ?? "true");
                var adminEmail = _config["EmailSettings:AdminEmail"] ?? "marc.coxall@ninjatec.co.uk";
                var fromEmail = _config["EmailSettings:SenderEmail"] ?? _config["EmailSettings__SenderEmail"] ?? "noreply@workouttracker.online";
                var senderName = _config["EmailSettings:SenderName"] ?? _config["EmailSettings__SenderName"] ?? "Workout Tracker";

                _logger.LogInformation($"Attempting to send feedback email using server: {host}:{port}, SSL: {enableSsl}");

                using var client = new SmtpClient(host)
                {
                    Port = port,
                    Credentials = new NetworkCredential(username, password),
                    EnableSsl = enableSsl
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, $"{senderName} Feedback"),
                    Subject = $"Workout Tracker Feedback: {subject}",
                    Body = $"<h3>New Feedback Submission</h3>" +
                           $"<p><strong>Subject:</strong> {subject}</p>" +
                           $"<p><strong>Message:</strong></p>" +
                           $"<p>{message.Replace(Environment.NewLine, "<br>")}</p>" +
                           (string.IsNullOrEmpty(senderEmail) ? "" : $"<p><strong>Contact Email:</strong> {senderEmail}</p>"),
                    IsBodyHtml = true
                };

                mailMessage.To.Add(adminEmail);

                // If reply is expected, set the reply-to header
                if (!string.IsNullOrEmpty(senderEmail))
                {
                    mailMessage.ReplyToList.Add(new MailAddress(senderEmail));
                }

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation($"Successfully sent feedback email with subject: {subject}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send feedback email: {ex.Message}");
                return false;
            }
        }
    }
}