namespace WorkoutTrackerWeb.Models.Configuration
{
    public class RedisConfiguration
    {
        public bool Enabled { get; set; }
        public string ConnectionString { get; set; }
        public string Password { get; set; }
        public string Instance { get; set; }
    }
}