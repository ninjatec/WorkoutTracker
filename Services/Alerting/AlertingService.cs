using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models.Alerting;
using WorkoutTrackerWeb.Services.Email;

namespace WorkoutTrackerWeb.Services.Alerting
{
    public class AlertingService : IAlertingService
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<AlertingService> _logger;

        public AlertingService(
            WorkoutTrackerWebContext context,
            IEmailService emailService,
            ILogger<AlertingService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Threshold configuration
        public async Task<IEnumerable<AlertThreshold>> GetAlertThresholdsAsync()
        {
            return await _context.AlertThreshold.ToListAsync();
        }

        public async Task<AlertThreshold> GetAlertThresholdAsync(int id)
        {
            return await _context.AlertThreshold.FindAsync(id);
        }

        public async Task<AlertThreshold> CreateAlertThresholdAsync(AlertThreshold threshold, string userName)
        {
            if (threshold == null)
                throw new ArgumentNullException(nameof(threshold));

            threshold.CreatedBy = userName;
            threshold.UpdatedBy = userName;
            threshold.CreatedAt = DateTime.UtcNow;
            threshold.UpdatedAt = DateTime.UtcNow;

            _context.AlertThreshold.Add(threshold);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Alert threshold created for metric {MetricName} by {UserName}", 
                threshold.MetricName, userName);

            return threshold;
        }

        public async Task<AlertThreshold> UpdateAlertThresholdAsync(AlertThreshold threshold, string userName)
        {
            if (threshold == null)
                throw new ArgumentNullException(nameof(threshold));

            var existingThreshold = await _context.AlertThreshold.FindAsync(threshold.Id);
            if (existingThreshold == null)
                throw new KeyNotFoundException($"Alert threshold with ID {threshold.Id} not found");

            // Update properties
            existingThreshold.MetricName = threshold.MetricName;
            existingThreshold.MetricCategory = threshold.MetricCategory;
            existingThreshold.WarningThreshold = threshold.WarningThreshold;
            existingThreshold.CriticalThreshold = threshold.CriticalThreshold;
            existingThreshold.Direction = threshold.Direction;
            existingThreshold.EmailEnabled = threshold.EmailEnabled;
            existingThreshold.NotificationEnabled = threshold.NotificationEnabled;
            existingThreshold.EscalationMinutes = threshold.EscalationMinutes;
            existingThreshold.Description = threshold.Description;
            existingThreshold.IsEnabled = threshold.IsEnabled;
            existingThreshold.UpdatedBy = userName;
            existingThreshold.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Alert threshold updated for metric {MetricName} by {UserName}", 
                threshold.MetricName, userName);

            return existingThreshold;
        }

        public async Task<bool> DeleteAlertThresholdAsync(int id)
        {
            var threshold = await _context.AlertThreshold.FindAsync(id);
            if (threshold == null)
                return false;

            _context.AlertThreshold.Remove(threshold);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Alert threshold deleted for metric {MetricName}", threshold.MetricName);

            return true;
        }

        // Alert management
        public async Task<IEnumerable<Alert>> GetActiveAlertsAsync()
        {
            return await _context.Alert
                .Include(a => a.AlertThreshold)
                .Where(a => a.ResolvedAt == null)
                .OrderByDescending(a => a.TriggeredAt)
                .ToListAsync();
        }

        public async Task<Alert> GetAlertAsync(int id)
        {
            return await _context.Alert
                .Include(a => a.AlertThreshold)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<bool> AcknowledgeAlertAsync(int id, string acknowledgedBy, string acknowledgementNote)
        {
            var alert = await _context.Alert.FindAsync(id);
            if (alert == null)
                return false;

            alert.IsAcknowledged = true;
            alert.AcknowledgedAt = DateTime.UtcNow;
            alert.AcknowledgedBy = acknowledgedBy;
            alert.AcknowledgementNote = acknowledgementNote;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Alert {AlertId} acknowledged by {UserName}", id, acknowledgedBy);

            return true;
        }

        public async Task<bool> ResolveAlertAsync(int id)
        {
            var alert = await _context.Alert.FindAsync(id);
            if (alert == null)
                return false;

            alert.ResolvedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Add to history when resolved
            await CreateAlertHistoryAsync(alert);

            _logger.LogInformation("Alert {AlertId} resolved", id);

            return true;
        }

        // Alert history
        public async Task<IEnumerable<AlertHistory>> GetAlertHistoryAsync(DateTime? from = null, DateTime? to = null, int? maxResults = null)
        {
            IQueryable<AlertHistory> query = _context.AlertHistory;

            if (from.HasValue)
                query = query.Where(h => h.TriggeredAt >= from.Value);

            if (to.HasValue)
                query = query.Where(h => h.TriggeredAt <= to.Value);

            query = query.OrderByDescending(h => h.TriggeredAt);

            if (maxResults.HasValue)
                query = query.Take(maxResults.Value);

            return await query.ToListAsync();
        }

        public async Task<AlertHistory> GetAlertHistoryItemAsync(int id)
        {
            return await _context.AlertHistory.FindAsync(id);
        }

        // Alert evaluation
        public async Task<bool> EvaluateMetricAsync(string metricName, string metricCategory, double value)
        {
            var thresholds = await _context.AlertThreshold
                .Where(t => t.MetricName == metricName && t.MetricCategory == metricCategory && t.IsEnabled)
                .ToListAsync();

            if (!thresholds.Any())
            {
                _logger.LogDebug("No thresholds found for metric {MetricName} in category {MetricCategory}", 
                    metricName, metricCategory);
                return false;
            }

            bool alertTriggered = false;

            foreach (var threshold in thresholds)
            {
                // Check if we already have an active alert for this threshold
                var existingAlert = await _context.Alert
                    .FirstOrDefaultAsync(a => a.AlertThresholdId == threshold.Id && a.ResolvedAt == null);

                // Evaluate if threshold is breached
                bool criticalBreached = IsThresholdBreached(value, threshold.CriticalThreshold, threshold.Direction);
                bool warningBreached = IsThresholdBreached(value, threshold.WarningThreshold, threshold.Direction);

                // If no threshold is breached and we have an existing alert, resolve it
                if (!criticalBreached && !warningBreached && existingAlert != null)
                {
                    existingAlert.ResolvedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    await CreateAlertHistoryAsync(existingAlert);
                    _logger.LogInformation("Alert for {MetricName} resolved as value {Value} is back to normal",
                        metricName, value);
                }
                // If a threshold is breached and we don't have an existing alert, create one
                else if ((criticalBreached || warningBreached) && existingAlert == null)
                {
                    var severity = criticalBreached ? AlertSeverity.Critical : AlertSeverity.Warning;
                    var alert = new Alert
                    {
                        AlertThresholdId = threshold.Id,
                        Severity = severity,
                        CurrentValue = value,
                        TriggeredAt = DateTime.UtcNow,
                        Details = $"Metric {metricName} value {value} breached {severity} threshold of " +
                                 $"{(severity == AlertSeverity.Critical ? threshold.CriticalThreshold : threshold.WarningThreshold)}"
                    };

                    _context.Alert.Add(alert);
                    await _context.SaveChangesAsync();

                    // Handle notifications
                    if (threshold.NotificationEnabled)
                    {
                        await CreateAlertNotificationsAsync(alert);
                        alert.NotificationSent = true;
                        alert.NotificationSentAt = DateTime.UtcNow;
                    }

                    // Handle email notifications
                    if (threshold.EmailEnabled)
                    {
                        await SendEmailForAlertAsync(alert);
                        alert.EmailSent = true;
                        alert.EmailSentAt = DateTime.UtcNow;
                    }

                    await _context.SaveChangesAsync();

                    _logger.LogWarning("New {Severity} alert created for {MetricName} with value {Value}",
                        severity, metricName, value);

                    alertTriggered = true;
                }
                // If a critical threshold is breached and we have an existing warning alert, upgrade it
                else if (criticalBreached && existingAlert != null && existingAlert.Severity == AlertSeverity.Warning)
                {
                    existingAlert.Severity = AlertSeverity.Critical;
                    existingAlert.CurrentValue = value;
                    existingAlert.Details = $"Metric {metricName} value {value} breached Critical threshold of {threshold.CriticalThreshold}";

                    await _context.SaveChangesAsync();

                    // Handle additional notifications for escalation
                    if (threshold.NotificationEnabled && !existingAlert.NotificationSent)
                    {
                        await CreateAlertNotificationsAsync(existingAlert);
                        existingAlert.NotificationSent = true;
                        existingAlert.NotificationSentAt = DateTime.UtcNow;
                    }

                    // Handle email notifications for escalation
                    if (threshold.EmailEnabled && !existingAlert.EmailSent)
                    {
                        await SendEmailForAlertAsync(existingAlert);
                        existingAlert.EmailSent = true;
                        existingAlert.EmailSentAt = DateTime.UtcNow;
                    }

                    await _context.SaveChangesAsync();

                    _logger.LogWarning("Alert for {MetricName} escalated to Critical with value {Value}",
                        metricName, value);

                    alertTriggered = true;
                }
                // Update value for existing alert if still breaching
                else if ((criticalBreached || warningBreached) && existingAlert != null)
                {
                    existingAlert.CurrentValue = value;
                    await _context.SaveChangesAsync();

                    // Check if we need to escalate based on time
                    if (!existingAlert.IsEscalated && threshold.EscalationMinutes.HasValue && 
                        !existingAlert.IsAcknowledged &&
                        DateTime.UtcNow > existingAlert.TriggeredAt.AddMinutes(threshold.EscalationMinutes.Value))
                    {
                        existingAlert.IsEscalated = true;
                        existingAlert.EscalatedAt = DateTime.UtcNow;
                        
                        // Send escalation notifications and emails
                        if (threshold.NotificationEnabled)
                        {
                            await CreateEscalationNotificationsAsync(existingAlert);
                        }
                        
                        if (threshold.EmailEnabled)
                        {
                            await SendEscalationEmailForAlertAsync(existingAlert);
                        }
                        
                        await _context.SaveChangesAsync();
                        
                        _logger.LogWarning("Alert for {MetricName} escalated after {Minutes} minutes without acknowledgement",
                            metricName, threshold.EscalationMinutes.Value);
                    }
                }
            }

            return alertTriggered;
        }

        public Task<bool> CheckAllThresholdsAsync()
        {
            // This method would be called by a background job to check all metrics
            // In a real implementation, you would iterate through all metrics and evaluate them
            _logger.LogInformation("Checking all alert thresholds...");
            
            // Mock implementation - in a real app, you would fetch actual metrics and evaluate them
            return Task.FromResult(false);
        }

        // Notification management
        public async Task<IEnumerable<Notification>> GetNotificationsForUserAsync(string userId, bool includeRead = false)
        {
            IQueryable<Notification> query = _context.Notification
                .Where(n => n.UserId == userId);

            if (!includeRead)
                query = query.Where(n => !n.IsRead);

            query = query.OrderByDescending(n => n.CreatedAt);

            return await query.ToListAsync();
        }

        public async Task<int> GetUnreadNotificationCountForUserAsync(string userId)
        {
            return await _context.Notification
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task<bool> MarkNotificationAsReadAsync(int notificationId)
        {
            var notification = await _context.Notification.FindAsync(notificationId);
            if (notification == null)
                return false;

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkAllNotificationsAsReadAsync(string userId)
        {
            var notifications = await _context.Notification
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            if (!notifications.Any())
                return false;

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        // Email notification
        public async Task<bool> SendEmailForAlertAsync(Alert alert)
        {
            try
            {
                var threshold = await _context.AlertThreshold.FindAsync(alert.AlertThresholdId);
                if (threshold == null)
                    return false;

                // In a real application, you'd get admin emails from configuration
                var adminEmails = new[] { "admin@workouttracker.app" };

                foreach (var email in adminEmails)
                {
                    string subject = $"{alert.Severity} Alert: {threshold.MetricName} in {threshold.MetricCategory}";
                    string message = $@"
                        <h2>{alert.Severity} Alert Triggered</h2>
                        <p><strong>Metric:</strong> {threshold.MetricName}</p>
                        <p><strong>Category:</strong> {threshold.MetricCategory}</p>
                        <p><strong>Current Value:</strong> {alert.CurrentValue}</p>
                        <p><strong>Threshold:</strong> {(alert.Severity == AlertSeverity.Critical ? threshold.CriticalThreshold : threshold.WarningThreshold)}</p>
                        <p><strong>Direction:</strong> {threshold.Direction}</p>
                        <p><strong>Time:</strong> {alert.TriggeredAt.ToString("yyyy-MM-dd HH:mm:ss")} UTC</p>
                        <p><strong>Details:</strong> {alert.Details}</p>
                        <p>Please check the <a href='https://workouttracker.app/Admin/Alerts'>Alert Dashboard</a> for more information.</p>
                    ";

                    await _emailService.SendEmailAsync(email, subject, message);
                }

                _logger.LogInformation("Alert email sent for {Severity} alert on {MetricName}",
                    alert.Severity, threshold.MetricName);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send alert email for alert {AlertId}", alert.Id);
                return false;
            }
        }

        // Helper methods
        private bool IsThresholdBreached(double value, double threshold, ThresholdDirection direction)
        {
            return direction switch
            {
                ThresholdDirection.Above => value > threshold,
                ThresholdDirection.Below => value < threshold,
                ThresholdDirection.Equal => Math.Abs(value - threshold) < 0.0001, // For floating point comparison
                ThresholdDirection.NotEqual => Math.Abs(value - threshold) >= 0.0001,
                _ => false
            };
        }

        private async Task CreateAlertHistoryAsync(Alert alert)
        {
            var threshold = await _context.AlertThreshold.FindAsync(alert.AlertThresholdId);
            if (threshold == null)
                return;

            var history = new AlertHistory
            {
                AlertId = alert.Id,
                MetricName = threshold.MetricName,
                MetricCategory = threshold.MetricCategory,
                Severity = alert.Severity,
                ThresholdValue = alert.Severity == AlertSeverity.Critical ? 
                    threshold.CriticalThreshold : threshold.WarningThreshold,
                ActualValue = alert.CurrentValue,
                Direction = threshold.Direction,
                TriggeredAt = alert.TriggeredAt,
                ResolvedAt = alert.ResolvedAt,
                WasAcknowledged = alert.IsAcknowledged,
                AcknowledgedAt = alert.AcknowledgedAt,
                AcknowledgedBy = alert.AcknowledgedBy,
                AcknowledgementNote = alert.AcknowledgementNote,
                WasEscalated = alert.IsEscalated,
                Details = alert.Details
            };

            if (alert.ResolvedAt.HasValue && alert.TriggeredAt != default)
            {
                history.TimeToResolve = alert.ResolvedAt.Value - alert.TriggeredAt;
            }

            if (alert.IsAcknowledged && alert.AcknowledgedAt.HasValue && alert.TriggeredAt != default)
            {
                history.TimeToAcknowledge = alert.AcknowledgedAt.Value - alert.TriggeredAt;
            }

            _context.AlertHistory.Add(history);
            await _context.SaveChangesAsync();
        }

        private async Task CreateAlertNotificationsAsync(Alert alert)
        {
            var threshold = await _context.AlertThreshold.FindAsync(alert.AlertThresholdId);
            if (threshold == null)
                return;

            // In a real application, you'd get admin user IDs from a role-based system
            var adminUserIds = new[] { "admin-user-id" };

            foreach (var userId in adminUserIds)
            {
                // Check if a notification already exists for this alert and user
                var existingNotification = await _context.Notification
                    .FirstOrDefaultAsync(n => n.AlertId == alert.Id && n.UserId == userId);
                
                // Skip if a notification already exists
                if (existingNotification != null)
                {
                    _logger.LogDebug("Notification already exists for Alert {AlertId} and User {UserId}", alert.Id, userId);
                    continue;
                }

                var notification = new Notification
                {
                    AlertId = alert.Id,
                    UserId = userId,
                    Title = $"{alert.Severity} Alert: {threshold.MetricName}",
                    Message = $"Metric {threshold.MetricName} value {alert.CurrentValue} has breached the {alert.Severity.ToString().ToLower()} threshold.",
                    Type = alert.Severity == AlertSeverity.Critical ? 
                        NotificationType.Critical : NotificationType.Warning,
                    Url = "/Admin/Alerts/Details/" + alert.Id
                };

                _context.Notification.Add(notification);
                _logger.LogInformation("Created notification for Alert {AlertId} and User {UserId}", alert.Id, userId);
            }

            await _context.SaveChangesAsync();
        }

        private async Task CreateEscalationNotificationsAsync(Alert alert)
        {
            var threshold = await _context.AlertThreshold.FindAsync(alert.AlertThresholdId);
            if (threshold == null)
                return;

            // In a real application, you'd get escalation user IDs (perhaps senior admins) from a role-based system
            var escalationUserIds = new[] { "senior-admin-user-id" };

            foreach (var userId in escalationUserIds)
            {
                // Check if an escalation notification already exists for this alert and user
                var existingNotification = await _context.Notification
                    .FirstOrDefaultAsync(n => n.AlertId == alert.Id && 
                                         n.UserId == userId && 
                                         n.Title.StartsWith("ESCALATED:"));
                
                // Skip if a notification already exists
                if (existingNotification != null)
                {
                    _logger.LogDebug("Escalation notification already exists for Alert {AlertId} and User {UserId}", alert.Id, userId);
                    continue;
                }

                var notification = new Notification
                {
                    AlertId = alert.Id,
                    UserId = userId,
                    Title = $"ESCALATED: {alert.Severity} Alert: {threshold.MetricName}",
                    Message = $"Escalated alert for {threshold.MetricName}. Value {alert.CurrentValue} has been in alert state for over {threshold.EscalationMinutes} minutes without acknowledgement.",
                    Type = NotificationType.Critical,
                    Url = "/Admin/Alerts/Details/" + alert.Id
                };

                _context.Notification.Add(notification);
                _logger.LogInformation("Created escalation notification for Alert {AlertId} and User {UserId}", alert.Id, userId);
            }

            await _context.SaveChangesAsync();
        }

        private async Task<bool> SendEscalationEmailForAlertAsync(Alert alert)
        {
            try
            {
                var threshold = await _context.AlertThreshold.FindAsync(alert.AlertThresholdId);
                if (threshold == null)
                    return false;

                // In a real application, you'd get escalation emails from configuration
                var escalationEmails = new[] { "operations@workouttracker.app", "manager@workouttracker.app" };

                foreach (var email in escalationEmails)
                {
                    string subject = $"ESCALATED: {alert.Severity} Alert: {threshold.MetricName} in {threshold.MetricCategory}";
                    string message = $@"
                        <h2>ESCALATED: {alert.Severity} Alert Requiring Attention</h2>
                        <p style='color: red; font-weight: bold;'>This alert has been escalated after {threshold.EscalationMinutes} minutes without acknowledgement.</p>
                        <p><strong>Metric:</strong> {threshold.MetricName}</p>
                        <p><strong>Category:</strong> {threshold.MetricCategory}</p>
                        <p><strong>Current Value:</strong> {alert.CurrentValue}</p>
                        <p><strong>Threshold:</strong> {(alert.Severity == AlertSeverity.Critical ? threshold.CriticalThreshold : threshold.WarningThreshold)}</p>
                        <p><strong>First Triggered:</strong> {alert.TriggeredAt.ToString("yyyy-MM-dd HH:mm:ss")} UTC</p>
                        <p><strong>Time since triggering:</strong> {DateTime.UtcNow.Subtract(alert.TriggeredAt).TotalMinutes:F1} minutes</p>
                        <p><strong>Details:</strong> {alert.Details}</p>
                        <p>Please check the <a href='https://workouttracker.app/Admin/Alerts'>Alert Dashboard</a> urgently.</p>
                    ";

                    await _emailService.SendEmailAsync(email, subject, message);
                }

                _logger.LogInformation("Escalation email sent for unacknowledged alert on {MetricName} after {Minutes} minutes",
                    threshold.MetricName, threshold.EscalationMinutes);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send escalation email for alert {AlertId}", alert.Id);
                return false;
            }
        }
    }
}