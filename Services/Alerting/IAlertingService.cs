using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WorkoutTrackerWeb.Models.Alerting;

namespace WorkoutTrackerWeb.Services.Alerting
{
    public interface IAlertingService
    {
        // Threshold configuration
        Task<IEnumerable<AlertThreshold>> GetAlertThresholdsAsync();
        Task<AlertThreshold> GetAlertThresholdAsync(int id);
        Task<AlertThreshold> CreateAlertThresholdAsync(AlertThreshold threshold, string userName);
        Task<AlertThreshold> UpdateAlertThresholdAsync(AlertThreshold threshold, string userName);
        Task<bool> DeleteAlertThresholdAsync(int id);
        
        // Alert management
        Task<IEnumerable<Alert>> GetActiveAlertsAsync();
        Task<Alert> GetAlertAsync(int id);
        Task<bool> AcknowledgeAlertAsync(int id, string acknowledgedBy, string acknowledgementNote);
        Task<bool> ResolveAlertAsync(int id);
        
        // Alert history
        Task<IEnumerable<AlertHistory>> GetAlertHistoryAsync(DateTime? from = null, DateTime? to = null, int? maxResults = null);
        Task<AlertHistory> GetAlertHistoryItemAsync(int id);
        
        // Alert evaluation
        Task<bool> EvaluateMetricAsync(string metricName, string metricCategory, double value);
        Task<bool> CheckAllThresholdsAsync(); // Batch operation for scheduled checks
        
        // Notification management
        Task<IEnumerable<Notification>> GetNotificationsForUserAsync(string userId, bool includeRead = false);
        Task<int> GetUnreadNotificationCountForUserAsync(string userId);
        Task<bool> MarkNotificationAsReadAsync(int notificationId);
        Task<bool> MarkAllNotificationsAsReadAsync(string userId);
        
        // Email notification
        Task<bool> SendEmailForAlertAsync(Alert alert);
    }
}