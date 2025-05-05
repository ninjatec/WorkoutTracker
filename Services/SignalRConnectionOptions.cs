using System;
using System.Collections.Generic;

namespace WorkoutTrackerWeb.Services
{
    /// <summary>
    /// Configuration options for SignalR connections
    /// </summary>
    public class SignalRConnectionOptions
    {
        /// <summary>
        /// Determines if the connection is for a mobile client
        /// </summary>
        public bool IsMobileClient { get; set; }
        
        /// <summary>
        /// Maximum number of reconnection attempts before giving up
        /// </summary>
        public int MaxReconnectAttempts { get; set; } = 5;
        
        /// <summary>
        /// Reconnection delay intervals in milliseconds
        /// </summary>
        public int[] ReconnectDelays { get; set; } = new[] { 0, 2000, 5000, 10000, 30000 };
        
        /// <summary>
        /// Connection keepalive interval in milliseconds
        /// </summary>
        public int KeepAliveIntervalMs { get; set; } = 15000;
        
        /// <summary>
        /// Server timeout in milliseconds
        /// </summary>
        public int ServerTimeoutMs { get; set; } = 30000;
        
        /// <summary>
        /// Whether to use long polling as a fallback
        /// </summary>
        public bool EnableLongPollingFallback { get; set; } = true;
        
        /// <summary>
        /// For mobile clients, whether to use a reduced heartbeat frequency to save battery
        /// </summary>
        public bool EnableReducedHeartbeat { get; set; } = true;

        /// <summary>
        /// Get appropriate reconnect delays based on the client type
        /// </summary>
        public int[] GetReconnectDelays()
        {
            if (IsMobileClient)
            {
                // For mobile clients, use longer delays to save battery
                return new[] { 0, 3000, 10000, 30000, 60000 };
            }
            
            return ReconnectDelays;
        }
        
        /// <summary>
        /// Get appropriate keepalive interval based on the client type
        /// </summary>
        public int GetKeepAliveInterval()
        {
            if (IsMobileClient && EnableReducedHeartbeat)
            {
                // Extend the keepalive interval for mobile clients to reduce battery usage
                return 30000; // 30 seconds
            }
            
            return KeepAliveIntervalMs;
        }
    }
}