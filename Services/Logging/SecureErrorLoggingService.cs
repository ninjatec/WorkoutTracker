using Microsoft.AspNetCore.Diagnostics;
using Serilog;
using Serilog.Events;
using WorkoutTrackerWeb.Models.Configuration;
using System.Text.Json;

namespace WorkoutTrackerWeb.Services.Logging
{
    /// <summary>
    /// Service for secure error logging that prevents information disclosure while maintaining
    /// comprehensive security monitoring capabilities for DAST compliance
    /// </summary>
    public interface ISecureErrorLoggingService
    {
        Task LogSecurityExceptionAsync(Exception exception, HttpContext context, string additionalContext = null);
        Task LogErrorPageAccessAsync(int statusCode, HttpContext context);
        Task LogSuspiciousActivityAsync(string activity, HttpContext context, string details = null);
    }

    public class SecureErrorLoggingService : ISecureErrorLoggingService
    {
        private readonly ErrorHandlingSecurityConfig _config;
        private readonly ILogger<SecureErrorLoggingService> _logger;

        public SecureErrorLoggingService(
            IConfiguration configuration,
            ILogger<SecureErrorLoggingService> logger)
        {
            _config = configuration.GetSection("ErrorHandlingSecurity").Get<ErrorHandlingSecurityConfig>() 
                      ?? new ErrorHandlingSecurityConfig();
            _logger = logger;
        }

        public Task LogSecurityExceptionAsync(Exception exception, HttpContext context, string additionalContext = null)
        {
            if (!_config.EnableDetailedLogging) return Task.CompletedTask;

            var logData = BuildSecureLogData(context, exception.GetType().Name);
            logData.Add("ExceptionMessage", SanitizeExceptionMessage(exception.Message));
            logData.Add("ExceptionType", exception.GetType().FullName);
            
            if (!string.IsNullOrEmpty(additionalContext))
            {
                logData.Add("AdditionalContext", additionalContext);
            }

            // Check if this is a security-sensitive exception type
            var isSecurityAlert = _config.EnableSecurityAlerts && 
                                  _config.SecurityAlertExceptionTypes.Any(type => 
                                      exception.GetType().FullName.Contains(type));

            if (isSecurityAlert)
            {
                Log.Error(exception, "SECURITY ALERT - Exception: {LogData}", JsonSerializer.Serialize(logData));
                _logger.LogCritical(exception, "Security Alert - Exception occurred: {LogData}", JsonSerializer.Serialize(logData));
            }
            else
            {
                Log.Error(exception, "Exception occurred: {LogData}", JsonSerializer.Serialize(logData));
                _logger.LogError(exception, "Exception occurred: {LogData}", JsonSerializer.Serialize(logData));
            }
            
            return Task.CompletedTask;
        }

        public Task LogErrorPageAccessAsync(int statusCode, HttpContext context)
        {
            var logData = BuildSecureLogData(context, $"HTTP{statusCode}");
            logData.Add("StatusCode", statusCode);
            logData.Add("EventType", "ErrorPageAccess");

            var logLevel = statusCode >= 500 ? LogEventLevel.Error : LogEventLevel.Warning;
            Log.Write(logLevel, "Error page accessed: {LogData}", JsonSerializer.Serialize(logData));
            
            if (statusCode >= 500)
            {
                _logger.LogError("Error page accessed: {LogData}", JsonSerializer.Serialize(logData));
            }
            else
            {
                _logger.LogWarning("Error page accessed: {LogData}", JsonSerializer.Serialize(logData));
            }
            
            return Task.CompletedTask;
        }

        public Task LogSuspiciousActivityAsync(string activity, HttpContext context, string details = null)
        {
            var logData = BuildSecureLogData(context, "SuspiciousActivity");
            logData.Add("Activity", activity);
            logData.Add("EventType", "SuspiciousActivity");
            
            if (!string.IsNullOrEmpty(details))
            {
                logData.Add("Details", details);
            }

            Log.Warning("SECURITY ALERT - Suspicious Activity: {LogData}", JsonSerializer.Serialize(logData));
            _logger.LogWarning("Suspicious Activity: {LogData}", JsonSerializer.Serialize(logData));
            
            return Task.CompletedTask;
        }

        private Dictionary<string, object> BuildSecureLogData(HttpContext context, string eventType)
        {
            var logData = new Dictionary<string, object>
            {
                ["Timestamp"] = DateTime.UtcNow,
                ["RequestId"] = context.TraceIdentifier,
                ["EventType"] = eventType,
                ["RequestPath"] = SanitizePath(context.Request.Path.ToString()),
                ["RequestMethod"] = context.Request.Method
            };

            if (_config.LogUserInformation)
            {
                logData["UserId"] = context.User?.Identity?.Name ?? "Anonymous";
                logData["IsAuthenticated"] = context.User?.Identity?.IsAuthenticated ?? false;
            }

            if (_config.LogIpAddresses)
            {
                logData["ClientIP"] = GetClientIpAddress(context);
                logData["ForwardedFor"] = context.Request.Headers["X-Forwarded-For"].ToString();
            }

            // Add safe request headers
            logData["UserAgent"] = context.Request.Headers["User-Agent"].ToString();
            logData["Referer"] = SanitizePath(context.Request.Headers["Referer"].ToString());
            
            // Add query string (but sanitize sensitive parameters)
            if (context.Request.QueryString.HasValue)
            {
                logData["QueryString"] = SanitizeQueryString(context.Request.QueryString.ToString());
            }

            return logData;
        }

        private string SanitizeExceptionMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return "No message";

            // Remove potentially sensitive information
            var sanitized = message;
            
            // Remove file paths
            sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, 
                @"[A-Za-z]:\\(?:[^\\/:*?""<>|\r\n]+\\)*[^\\/:*?""<>|\r\n]*", "[FILE_PATH]");
            
            // Remove connection strings
            sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, 
                @"(server|data source|initial catalog|user id|password)\s*=\s*[^;]+", 
                "$1=[REDACTED]", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Truncate if too long
            if (sanitized.Length > _config.MaxErrorMessageLength)
            {
                sanitized = sanitized.Substring(0, _config.MaxErrorMessageLength) + "...[TRUNCATED]";
            }

            return sanitized;
        }

        private string SanitizePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;

            // Check if path contains sensitive patterns
            foreach (var sensitivePattern in _config.SensitivePathPatterns)
            {
                if (path.Contains(sensitivePattern, StringComparison.OrdinalIgnoreCase))
                {
                    return "[SENSITIVE_PATH]";
                }
            }

            return path;
        }

        private string SanitizeQueryString(string queryString)
        {
            if (string.IsNullOrEmpty(queryString)) return string.Empty;

            // Remove potentially sensitive query parameters
            var sensitiveParams = new[] { "password", "token", "key", "secret", "auth" };
            var sanitized = queryString;

            foreach (var param in sensitiveParams)
            {
                sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized,
                    $@"({param}=)[^&]*", "$1[REDACTED]", 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }

            return sanitized;
        }

        private string GetClientIpAddress(HttpContext context)
        {
            // Try to get the real IP address, considering proxies
            var ip = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (string.IsNullOrEmpty(ip))
            {
                ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            }
            if (string.IsNullOrEmpty(ip))
            {
                ip = context.Connection.RemoteIpAddress?.ToString();
            }

            return ip ?? "Unknown";
        }
    }
}
