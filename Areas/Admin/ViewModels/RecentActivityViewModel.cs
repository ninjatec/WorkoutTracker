using System;

namespace WorkoutTrackerWeb.Areas.Admin.ViewModels
{
    public class RecentActivityViewModel
    {
        public string Username { get; set; }
        public string ActivityType { get; set; }
        public string ActivityDescription { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
