using System;
using StackExchange.Redis;

namespace WorkoutTrackerWeb.Services.Redis
{
    public class RedisConfiguration
    {
        public bool Enabled { get; set; }
        public string ConnectionString { get; set; }
        public int DatabaseId { get; set; } = 0;
        public int ConnectTimeout { get; set; } = 5000;
        public int ConnectRetry { get; set; } = 3;
        public bool AbortOnConnectFail { get; set; } = false;

        public ConfigurationOptions ToConfigurationOptions()
        {
            try
            {
                // Extract the host:port part
                string hostPort = ConnectionString;
                if (ConnectionString.Contains(","))
                {
                    hostPort = ConnectionString.Substring(0, ConnectionString.IndexOf(","));
                }
                
                // Attempt to parse the connection string
                var options = ConfigurationOptions.Parse(ConnectionString);
                
                // Ensure we have the right endpoint
                options.EndPoints.Clear();
                options.EndPoints.Add(hostPort);
                
                // Set additional parameters
                options.DefaultDatabase = DatabaseId;
                options.ConnectTimeout = ConnectTimeout;
                options.ConnectRetry = ConnectRetry;
                options.AbortOnConnectFail = AbortOnConnectFail;
                
                return options;
            }
            catch (Exception)
            {
                // Fallback - create a configuration from scratch
                return new ConfigurationOptions
                {
                    EndPoints = { ConnectionString.Split(',')[0] },
                    DefaultDatabase = DatabaseId,
                    ConnectTimeout = ConnectTimeout,
                    ConnectRetry = ConnectRetry,
                    AbortOnConnectFail = AbortOnConnectFail
                };
            }
        }
    }
}