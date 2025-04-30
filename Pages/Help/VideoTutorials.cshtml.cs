using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;

namespace WorkoutTrackerWeb.Pages.Help
{
    [OutputCache(PolicyName = "StaticContent")]
    public class VideoTutorialsModel : PageModel
    {
        public class VideoTutorial
        {
            public string Title { get; set; }
            public string Description { get; set; }
            public string EmbedUrl { get; set; }
            public string Duration { get; set; }
            public string Category { get; set; }
        }

        public List<VideoTutorial> Videos { get; set; }
        public List<string> VideoCategories { get; set; }

        public void OnGet()
        {
            VideoCategories = new List<string>
            {
                "Getting Started",
                "Workout Sessions",
                "Exercise Tracking",
                "Progress Reports",
                "Account Settings",
                "Coaching",
                "Sharing"
            };

            Videos = new List<VideoTutorial>
            {
                new VideoTutorial
                {
                    Title = "Getting Started with Workout Tracker",
                    Description = "Learn the basics of Workout Tracker and how to set up your account.",
                    EmbedUrl = "https://www.youtube.com/embed/dQw4w9WgXcQ", // Placeholder URL
                    Duration = "2:30",
                    Category = "Getting Started"
                },
                new VideoTutorial
                {
                    Title = "Creating Your First Workout Session",
                    Description = "A step-by-step guide to creating your first workout session.",
                    EmbedUrl = "https://www.youtube.com/embed/dQw4w9WgXcQ", // Placeholder URL
                    Duration = "3:15",
                    Category = "Getting Started"
                },
                new VideoTutorial
                {
                    Title = "Adding Sets and Reps",
                    Description = "Learn how to add sets and repetitions to your workout sessions.",
                    EmbedUrl = "https://www.youtube.com/embed/dQw4w9WgXcQ", // Placeholder URL
                    Duration = "4:45",
                    Category = "Exercise Tracking"
                },
                new VideoTutorial
                {
                    Title = "Managing Exercise Types",
                    Description = "How to create, edit, and organize your exercise library.",
                    EmbedUrl = "https://www.youtube.com/embed/dQw4w9WgXcQ", // Placeholder URL
                    Duration = "3:50",
                    Category = "Exercise Tracking"
                },
                new VideoTutorial
                {
                    Title = "Tracking Your Progress",
                    Description = "See how to use the Reports section to track your fitness progress over time.",
                    EmbedUrl = "https://www.youtube.com/embed/dQw4w9WgXcQ", // Placeholder URL
                    Duration = "5:10",
                    Category = "Progress Reports"
                },
                new VideoTutorial
                {
                    Title = "Understanding One Rep Max",
                    Description = "Learn what the One Rep Max calculation is and how to use it effectively.",
                    EmbedUrl = "https://www.youtube.com/embed/dQw4w9WgXcQ", // Placeholder URL
                    Duration = "4:20",
                    Category = "Progress Reports"
                },
                new VideoTutorial
                {
                    Title = "Managing Workout Sessions",
                    Description = "How to edit, delete, and organize your workout sessions.",
                    EmbedUrl = "https://www.youtube.com/embed/dQw4w9WgXcQ", // Placeholder URL
                    Duration = "3:30",
                    Category = "Workout Sessions"
                },
                new VideoTutorial
                {
                    Title = "Cloning Previous Workouts",
                    Description = "Save time by learning how to clone and modify previous workout sessions.",
                    EmbedUrl = "https://www.youtube.com/embed/dQw4w9WgXcQ", // Placeholder URL
                    Duration = "2:45",
                    Category = "Workout Sessions"
                },
                new VideoTutorial
                {
                    Title = "Customizing Your Profile",
                    Description = "How to update your profile settings and preferences.",
                    EmbedUrl = "https://www.youtube.com/embed/dQw4w9WgXcQ", // Placeholder URL
                    Duration = "3:15",
                    Category = "Account Settings"
                },
                new VideoTutorial
                {
                    Title = "Security and Privacy Settings",
                    Description = "Learn how to manage your account security and privacy options.",
                    EmbedUrl = "https://www.youtube.com/embed/dQw4w9WgXcQ", // Placeholder URL
                    Duration = "4:00",
                    Category = "Account Settings"
                },
                new VideoTutorial
                {
                    Title = "Getting Started as a Coach",
                    Description = "Learn how to set up your coaching profile and start accepting clients.",
                    EmbedUrl = "https://www.youtube.com/embed/dQw4w9WgXcQ",
                    Duration = "5:30",
                    Category = "Coaching"
                },
                new VideoTutorial
                {
                    Title = "Managing Your Client Roster",
                    Description = "A comprehensive guide to managing your coaching clients and groups.",
                    EmbedUrl = "https://www.youtube.com/embed/dQw4w9WgXcQ",
                    Duration = "6:15",
                    Category = "Coaching"
                },
                new VideoTutorial
                {
                    Title = "Creating and Assigning Workout Templates",
                    Description = "Learn how to create effective workout templates and assign them to clients.",
                    EmbedUrl = "https://www.youtube.com/embed/dQw4w9WgXcQ",
                    Duration = "4:45",
                    Category = "Coaching"
                },
                new VideoTutorial
                {
                    Title = "Tracking Client Progress",
                    Description = "How to monitor and analyze your clients' workout progress.",
                    EmbedUrl = "https://www.youtube.com/embed/dQw4w9WgXcQ",
                    Duration = "5:00",
                    Category = "Coaching"
                },
                new VideoTutorial
                {
                    Title = "Sharing Your Workout Data",
                    Description = "Learn how to securely share your workout data with others.",
                    EmbedUrl = "https://www.youtube.com/embed/dQw4w9WgXcQ",
                    Duration = "3:30",
                    Category = "Sharing"
                },
                new VideoTutorial
                {
                    Title = "Managing Shared Access",
                    Description = "Control who can see your workout data and for how long.",
                    EmbedUrl = "https://www.youtube.com/embed/dQw4w9WgXcQ",
                    Duration = "4:00",
                    Category = "Sharing"
                }
            };
        }
    }
}