using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Models.Alerting;
using WorkoutTrackerWeb.Services.Alerting;

namespace WorkoutTrackerWeb.Services.Hangfire
{
    /// <summary>
    /// Fallback implementation of IAlertingService that doesn't depend on any database or external services.
    /// Used when proper dependency injection can't be performed to avoid exceptions.
    /// </summary>
    internal class FallbackAlertingService : IAlertingService
    {
        private readonly ILogger _logger;

        public FallbackAlertingService(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<bool> AcknowledgeAlertAsync(int id, string acknowledgedBy, string acknowledgementNote)
        {
            _logger.LogWarning("FallbackAlertingService: AcknowledgeAlertAsync called without full implementation");
            return Task.FromResult(false);
        }

        public Task<bool> CheckAllThresholdsAsync()
        {
            _logger.LogWarning("FallbackAlertingService: CheckAllThresholdsAsync called without full implementation");
            return Task.FromResult(false);
        }

        public Task<AlertThreshold> CreateAlertThresholdAsync(AlertThreshold threshold, string userName)
        {
            _logger.LogWarning("FallbackAlertingService: CreateAlertThresholdAsync called without full implementation");
            return Task.FromResult<AlertThreshold>(null);
        }

        public Task<bool> DeleteAlertThresholdAsync(int id)
        {
            _logger.LogWarning("FallbackAlertingService: DeleteAlertThresholdAsync called without full implementation");
            return Task.FromResult(false);
        }

        public Task<bool> EvaluateMetricAsync(string metricName, string metricCategory, double value)
        {
            _logger.LogWarning("FallbackAlertingService: EvaluateMetricAsync called without full implementation. Metric: {MetricName}, Value: {Value}", metricName, value);
            return Task.FromResult(false);
        }

        public Task<IEnumerable<Alert>> GetActiveAlertsAsync()
        {
            _logger.LogWarning("FallbackAlertingService: GetActiveAlertsAsync called without full implementation");
            return Task.FromResult<IEnumerable<Alert>>(Array.Empty<Alert>());
        }

        public Task<Alert> GetAlertAsync(int id)
        {
            _logger.LogWarning("FallbackAlertingService: GetAlertAsync called without full implementation");
            return Task.FromResult<Alert>(null);
        }

        public Task<IEnumerable<AlertHistory>> GetAlertHistoryAsync(DateTime? from = null, DateTime? to = null, int? maxResults = null)
        {
            _logger.LogWarning("FallbackAlertingService: GetAlertHistoryAsync called without full implementation");
            return Task.FromResult<IEnumerable<AlertHistory>>(Array.Empty<AlertHistory>());
        }

        public Task<AlertHistory> GetAlertHistoryItemAsync(int id)
        {
            _logger.LogWarning("FallbackAlertingService: GetAlertHistoryItemAsync called without full implementation");
            return Task.FromResult<AlertHistory>(null);
        }

        public Task<AlertThreshold> GetAlertThresholdAsync(int id)
        {
            _logger.LogWarning("FallbackAlertingService: GetAlertThresholdAsync called without full implementation");
            return Task.FromResult<AlertThreshold>(null);
        }

        public Task<IEnumerable<AlertThreshold>> GetAlertThresholdsAsync()
        {
            _logger.LogWarning("FallbackAlertingService: GetAlertThresholdsAsync called without full implementation");
            return Task.FromResult<IEnumerable<AlertThreshold>>(Array.Empty<AlertThreshold>());
        }

        public Task<IEnumerable<Notification>> GetNotificationsForUserAsync(string userId, bool includeRead = false)
        {
            _logger.LogWarning("FallbackAlertingService: GetNotificationsForUserAsync called without full implementation");
            return Task.FromResult<IEnumerable<Notification>>(Array.Empty<Notification>());
        }

        public Task<int> GetUnreadNotificationCountForUserAsync(string userId)
        {
            _logger.LogWarning("FallbackAlertingService: GetUnreadNotificationCountForUserAsync called without full implementation");
            return Task.FromResult(0);
        }

        public Task<bool> MarkAllNotificationsAsReadAsync(string userId)
        {
            _logger.LogWarning("FallbackAlertingService: MarkAllNotificationsAsReadAsync called without full implementation");
            return Task.FromResult(false);
        }

        public Task<bool> MarkNotificationAsReadAsync(int notificationId)
        {
            _logger.LogWarning("FallbackAlertingService: MarkNotificationAsReadAsync called without full implementation");
            return Task.FromResult(false);
        }

        public Task<bool> ResolveAlertAsync(int id)
        {
            _logger.LogWarning("FallbackAlertingService: ResolveAlertAsync called without full implementation");
            return Task.FromResult(false);
        }

        public Task<bool> SendEmailForAlertAsync(Alert alert)
        {
            _logger.LogWarning("FallbackAlertingService: SendEmailForAlertAsync called without full implementation");
            return Task.FromResult(false);
        }

        public Task<AlertThreshold> UpdateAlertThresholdAsync(AlertThreshold threshold, string userName)
        {
            _logger.LogWarning("FallbackAlertingService: UpdateAlertThresholdAsync called without full implementation");
            return Task.FromResult<AlertThreshold>(null);
        }
    }
}