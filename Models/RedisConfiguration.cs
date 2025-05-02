using System.Collections.Generic;

namespace WorkoutTrackerWeb.Models
{
    public class RedisConfiguration
    {
        public string ConnectionString { get; set; }
        public List<string> Endpoints { get; set; }
        public bool UseDistributedCache { get; set; }
        public int DatabaseId { get; set; }
    }
}