namespace WorkoutTrackerWeb.Models.Configuration
{
    /// <summary>
    /// Configuration for security-focused error handling to prevent information disclosure
    /// </summary>
    public class ErrorHandlingSecurityConfig
    {
        /// <summary>
        /// Whether to log detailed exception information for security monitoring
        /// </summary>
        public bool EnableDetailedLogging { get; set; } = true;

        /// <summary>
        /// Whether to include request ID in error responses for support purposes
        /// </summary>
        public bool IncludeRequestIdInResponse { get; set; } = true;

        /// <summary>
        /// Whether to log user information in error logs for security monitoring
        /// </summary>
        public bool LogUserInformation { get; set; } = true;

        /// <summary>
        /// Whether to log IP addresses in error logs for security analysis
        /// </summary>
        public bool LogIpAddresses { get; set; } = true;

        /// <summary>
        /// Maximum number of characters allowed in error messages shown to users
        /// </summary>
        public int MaxErrorMessageLength { get; set; } = 200;

        /// <summary>
        /// List of exception types that should trigger security alerts
        /// </summary>
        public List<string> SecurityAlertExceptionTypes { get; set; } = new()
        {
            "System.Security.SecurityException",
            "System.UnauthorizedAccessException",
            "Microsoft.Data.SqlClient.SqlException",
            "System.Data.SqlClient.SqlException",
            "System.ArgumentException",
            "System.InvalidOperationException"
        };

        /// <summary>
        /// Whether to send security alerts for specific exception types
        /// </summary>
        public bool EnableSecurityAlerts { get; set; } = true;

        /// <summary>
        /// Paths that should not be logged in error messages for security reasons
        /// </summary>
        public List<string> SensitivePathPatterns { get; set; } = new()
        {
            "/admin",
            "/api/admin",
            "/hangfire",
            "/.env",
            "/config"
        };
    }
}
