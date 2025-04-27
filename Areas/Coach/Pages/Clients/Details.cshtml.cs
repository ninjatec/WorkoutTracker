using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkoutTrackerWeb.Attributes;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models.Coaching;
using WorkoutTrackerWeb.Models.Identity;
using WorkoutTrackerWeb.Services.Coaching;

namespace WorkoutTrackerWeb.Areas.Coach.Pages.Clients
{
    [Area("Coach")]
    [CoachAuthorize("CanViewWorkouts")]
    public class DetailsModel : PageModel
    {
        private readonly ICoachingService _coachingService;
        private readonly WorkoutTrackerWebContext _context;
        private readonly UserManager<AppUser> _userManager;

        public DetailsModel(
            ICoachingService coachingService,
            WorkoutTrackerWebContext context,
            UserManager<AppUser> userManager)
        {
            _coachingService = coachingService;
            _context = context;
            _userManager = userManager;
        }

        [TempData]
        public string StatusMessage { get; set; }

        [TempData]
        public string StatusMessageType { get; set; } = "Success";

        public int ClientId { get; set; }
        public string ClientName { get; set; }
        public string ClientEmail { get; set; }
        public string ClientGroup { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime LastActive { get; set; }
        public bool CanViewPersonalInfo { get; set; }
        
        // Personal info properties (if permission allows)
        public int Age { get; set; }
        public decimal Height { get; set; }
        public decimal Weight { get; set; }
        public decimal BMI { get; set; }
        
        // Activity and metrics
        public int TotalWorkouts { get; set; }
        public int WorkoutsThisMonth { get; set; }
        public int ActiveGoals { get; set; }
        public int CompletedGoals { get; set; }
        public int Consistency { get; set; }
        public List<RecentWorkoutViewModel> RecentWorkouts { get; set; } = new List<RecentWorkoutViewModel>();
        public List<GoalViewModel> GoalsList { get; set; } = new List<GoalViewModel>();
        
        // Performance metrics
        public List<int> VolumeData { get; set; } = new List<int>();
        public List<TopExerciseViewModel> TopExercises { get; set; } = new List<TopExerciseViewModel>();
        
        // Communication
        public List<CoachNoteViewModel> CoachNotes { get; set; } = new List<CoachNoteViewModel>();
        public List<MessageViewModel> RecentMessages { get; set; } = new List<MessageViewModel>();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var coachId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(coachId))
            {
                return Forbid();
            }

            // Get the relationship
            var relationship = await _context.CoachClientRelationships
                .Include(r => r.Client)
                .Include(r => r.Permissions)
                .FirstOrDefaultAsync(r => r.Id == id && r.CoachId == coachId);

            if (relationship == null)
            {
                return NotFound("Client relationship not found.");
            }

            // Set basic client information
            ClientId = relationship.Id;
            ClientName = relationship.Client?.UserName?.Split('@')[0] ?? "Client";
            ClientEmail = relationship.Client?.Email ?? "N/A";
            StartDate = relationship.StartDate ?? relationship.CreatedDate;
            LastActive = DateTime.Now.AddDays(-new Random().Next(0, 5)); // For demo purposes
            
            // Check permissions
            CanViewPersonalInfo = relationship.Permissions?.CanViewPersonalInfo ?? false;
            
            // Set client group (placeholder for demo)
            ClientGroup = GetSampleClientGroup(id);
            
            // Load demo data
            LoadDemoData();

            return Page();
        }

        private string GetSampleClientGroup(int id)
        {
            // For demo purposes, assign some sample groups
            var groups = new[] { "Strength Training", "Weight Loss", "Marathon Prep", "Beginners" };
            var random = new Random(id); // Use ID as seed for consistent results
            return random.Next(0, 10) < 7 ? groups[random.Next(0, groups.Length)] : string.Empty;
        }

        private void LoadDemoData()
        {
            // For demo purposes, populate with sample data
            var random = new Random(ClientId); // Use client ID as seed for consistent results
            
            // Personal information (if permission allows)
            if (CanViewPersonalInfo)
            {
                Age = random.Next(18, 65);
                Height = 150 + (decimal)(random.NextDouble() * 50);
                Weight = 50 + (decimal)(random.NextDouble() * 70);
                BMI = Math.Round(Weight / ((Height / 100) * (Height / 100)), 1);
            }
            
            // Activity metrics
            TotalWorkouts = random.Next(10, 100);
            WorkoutsThisMonth = random.Next(0, 15);
            ActiveGoals = random.Next(1, 5);
            CompletedGoals = random.Next(0, 10);
            Consistency = random.Next(30, 95);
            
            // Volume data for chart
            for (int i = 0; i < 6; i++)
            {
                VolumeData.Add(2000 + random.Next(-500, 500));
            }
            
            // Recent workouts (last 5)
            var workoutNames = new[] { "Full Body", "Upper Body", "Lower Body", "Push", "Pull", "Legs", "Cardio", "HIIT" };
            var today = DateTime.Now;
            
            for (int i = 0; i < 5; i++)
            {
                RecentWorkouts.Add(new RecentWorkoutViewModel
                {
                    Date = today.AddDays(-random.Next(1, 30)),
                    Name = workoutNames[random.Next(0, workoutNames.Length)],
                    Duration = random.Next(30, 120),
                    Volume = 1000 + random.Next(0, 2000)
                });
            }
            
            // Sort workouts by date (most recent first)
            RecentWorkouts = RecentWorkouts.OrderByDescending(w => w.Date).ToList();
            
            // Top exercises
            var exerciseNames = new[] { "Bench Press", "Squat", "Deadlift", "Pull-ups", "Shoulder Press", "Leg Press" };
            for (int i = 0; i < 5; i++)
            {
                TopExercises.Add(new TopExerciseViewModel
                {
                    Name = exerciseNames[i],
                    PersonalBest = (i == 0 || i == 1) ? $"{random.Next(60, 150)} kg" : $"{random.Next(5, 20)} reps",
                    LastPerformed = today.AddDays(-random.Next(1, 60))
                });
            }
            
            // Active goals
            var goalDescriptions = new[]
            {
                "Increase bench press 1RM",
                "Run 5K under 25 minutes",
                "Complete 10 pull-ups",
                "Lose 5kg",
                "Improve squat form",
                "Increase deadlift to 100kg"
            };
            
            var goalCategories = new[] { "Strength", "Cardio", "Hypertrophy", "Weight", "Technique" };
            
            for (int i = 0; i < ActiveGoals; i++)
            {
                var targetDate = today.AddDays(random.Next(7, 90));
                var progress = random.Next(0, 100);
                
                GoalsList.Add(new GoalViewModel
                {
                    Description = goalDescriptions[random.Next(0, goalDescriptions.Length)],
                    Category = goalCategories[random.Next(0, goalCategories.Length)],
                    TargetDate = targetDate,
                    Progress = progress
                });
            }
            
            // Coach notes
            var noteContents = new[]
            {
                "Client showing good progress on upper body strength exercises.",
                "Discussed nutrition plan and protein intake goals.",
                "Form is improving on squats but needs continued monitoring.",
                "Client reported knee discomfort during lunges, modified exercise selection.",
                "Very consistent with workouts this month, consider increasing intensity."
            };
            
            for (int i = 0; i < 3; i++)
            {
                if (noteContents.Length > i)
                {
                    CoachNotes.Add(new CoachNoteViewModel
                    {
                        Date = today.AddDays(-random.Next(1, 60)),
                        Content = noteContents[i],
                        IsVisibleToClient = random.Next(0, 2) == 0
                    });
                }
            }
            
            // Sort notes by date (most recent first)
            CoachNotes = CoachNotes.OrderByDescending(n => n.Date).ToList();
            
            // Recent messages (last 3)
            var messageContents = new[]
            {
                "Great job on completing all your workouts this week!",
                "How did your leg day go? Were you able to increase the weight?",
                "I've updated your program with the modifications we discussed.",
                "Looking forward to our check-in session tomorrow.",
                "Remember to log your weight and measurements this week."
            };
            
            var clientResponses = new[]
            {
                "Thanks coach! Feeling good about my progress.",
                "Leg day was tough but I managed to add 5kg to my squat!",
                "Got it, I'll try the new program tomorrow.",
                "See you at 3pm for our check-in.",
                "Will do, just logged everything in the app."
            };
            
            for (int i = 0; i < 5; i++)
            {
                if (i < messageContents.Length)
                {
                    var isFromCoach = i % 2 == 0;
                    RecentMessages.Add(new MessageViewModel
                    {
                        Date = today.AddDays(-(i/2)).AddHours(-random.Next(1, 24)),
                        Content = isFromCoach ? messageContents[i] : clientResponses[i],
                        IsFromCoach = isFromCoach,
                        IsRead = random.Next(0, 5) > 1
                    });
                }
            }
            
            // Sort messages by date (most recent first)
            RecentMessages = RecentMessages.OrderByDescending(m => m.Date).ToList();
        }

        public async Task<IActionResult> OnPostAddNoteAsync(string noteContent, bool isVisibleToClient = false)
        {
            if (string.IsNullOrEmpty(noteContent))
            {
                StatusMessage = "Error: Note content cannot be empty.";
                StatusMessageType = "Error";
                return RedirectToPage();
            }
            
            var clientId = RouteData.Values["id"]?.ToString();
            if (string.IsNullOrEmpty(clientId))
            {
                StatusMessage = "Error: Client ID not found.";
                StatusMessageType = "Error";
                return RedirectToPage();
            }
            
            // In a real implementation, you would save this note to the database
            // For now, just return a success message
            StatusMessage = "Success: Note added successfully.";
            StatusMessageType = "Success";
            
            return RedirectToPage(new { id = clientId });
        }

        public async Task<IActionResult> OnPostSetGoalAsync(string goalDescription, string goalCategory, 
            DateTime targetDate, string measurementType, decimal startValue, decimal targetValue)
        {
            if (string.IsNullOrEmpty(goalDescription))
            {
                StatusMessage = "Error: Goal description cannot be empty.";
                StatusMessageType = "Error";
                return RedirectToPage();
            }
            
            var clientId = RouteData.Values["id"]?.ToString();
            if (string.IsNullOrEmpty(clientId))
            {
                StatusMessage = "Error: Client ID not found.";
                StatusMessageType = "Error";
                return RedirectToPage();
            }
            
            // In a real implementation, you would save this goal to the database
            // For now, just return a success message
            StatusMessage = "Success: Goal created successfully.";
            StatusMessageType = "Success";
            
            return RedirectToPage(new { id = clientId });
        }

        public class RecentWorkoutViewModel
        {
            public DateTime Date { get; set; }
            public string Name { get; set; }
            public int Duration { get; set; }
            public int Volume { get; set; }
        }

        public class GoalViewModel
        {
            public string Description { get; set; }
            public string Category { get; set; }
            public DateTime TargetDate { get; set; }
            public int Progress { get; set; }
        }

        public class TopExerciseViewModel
        {
            public string Name { get; set; }
            public string PersonalBest { get; set; }
            public DateTime LastPerformed { get; set; }
        }

        public class CoachNoteViewModel
        {
            public DateTime Date { get; set; }
            public string Content { get; set; }
            public bool IsVisibleToClient { get; set; }
        }

        public class MessageViewModel
        {
            public DateTime Date { get; set; }
            public string Content { get; set; }
            public bool IsFromCoach { get; set; }
            public bool IsRead { get; set; }
        }
    }
}