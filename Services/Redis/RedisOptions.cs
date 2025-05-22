using System;
using System.Collections.Generic;

namespace WorkoutTrackerWeb.Services.Redis
{
    public class RedisOptions
    {
        public string ConnectionString { get; set; }
        public string Instance { get; set; }
        public bool Enabled { get; set; }
        public string Password { get; set; }
        public List<string> Endpoints { get; set; }
        public int ConnectTimeout { get; set; } = 5000;
        public int SyncTimeout { get; set; } = 5000;
        public bool AbortOnConnectFail { get; set; } = false;
    }
}