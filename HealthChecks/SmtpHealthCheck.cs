using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WorkoutTrackerWeb.Services.Email;

namespace WorkoutTrackerWeb.HealthChecks
{
    /// <summary>
    /// Health check that tests SMTP connectivity for email services.
    /// </summary>
    public class SmtpHealthCheck : IHealthCheck
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<SmtpHealthCheck> _logger;

        public SmtpHealthCheck(
            IOptions<EmailSettings> emailSettings,
            ILogger<SmtpHealthCheck> logger)
        {
            _emailSettings = emailSettings?.Value ?? throw new ArgumentNullException(nameof(emailSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var data = new Dictionary<string, object>
            {
                { "SmtpServer", _emailSettings.MailServer },
                { "SmtpPort", _emailSettings.MailPort },
                { "UseSsl", _emailSettings.UseSsl },
                { "LastChecked", DateTime.UtcNow }
            };

            try
            {
                _logger.LogDebug("Checking SMTP connection health at {Server}:{Port}", 
                    _emailSettings.MailServer, _emailSettings.MailPort);
                
                using var client = new SmtpClient();
                
                // Determine appropriate security options
                SecureSocketOptions secureOption = _emailSettings.MailPort == 587 
                    ? SecureSocketOptions.StartTls 
                    : _emailSettings.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.None;
                
                // Set short timeout for health check
                client.Timeout = 5000; // 5 seconds
                
                // Connect to the SMTP server
                await client.ConnectAsync(_emailSettings.MailServer, _emailSettings.MailPort, secureOption, cancellationToken);
                
                // Authenticate if credentials are provided
                if (!string.IsNullOrEmpty(_emailSettings.UserName))
                {
                    await client.AuthenticateAsync(_emailSettings.UserName, _emailSettings.Password, cancellationToken);
                    data.Add("Authentication", "Successful");
                }
                
                // Disconnect
                await client.DisconnectAsync(true, cancellationToken);
                
                _logger.LogInformation("SMTP health check successful");
                return HealthCheckResult.Healthy("SMTP connection is healthy", data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SMTP health check failed: {ErrorMessage}", ex.Message);
                
                data.Add("Error", ex.Message);
                data.Add("ErrorType", ex.GetType().Name);
                
                return HealthCheckResult.Unhealthy("SMTP connection failed", ex, data);
            }
        }
    }
}