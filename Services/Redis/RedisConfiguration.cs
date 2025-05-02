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
            return new ConfigurationOptions
            {
                EndPoints = { ConnectionString },
                DefaultDatabase = DatabaseId,
                ConnectTimeout = ConnectTimeout,
                ConnectRetry = ConnectRetry,
                AbortOnConnectFail = AbortOnConnectFail
            };
        }
    }
}