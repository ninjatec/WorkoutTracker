using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerweb.Data;

namespace WorkoutTrackerWeb.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var context = new WorkoutTrackerWebContext(
                serviceProvider.GetRequiredService<DbContextOptions<WorkoutTrackerWebContext>>(),
                serviceProvider.GetService<Microsoft.AspNetCore.Http.IHttpContextAccessor>());

            // Get the application DB context for version management
            using var appDbContext = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>());

            // Initialize roles and admin users first
            await InitializeRolesAndAdminUser(serviceProvider);
            
            // Seed initial application version
            await SeedInitialVersion(appDbContext);
            
            // Seed exercise types
            await SeedExerciseTypes(context);
            
            // Seed set types
            await SeedSetTypes(context);
            
            // Seed help categories and articles
            await SeedHelpContent(context);
            
            // Seed glossary terms
            await SeedGlossaryTerms(context);
        }

        private static async Task InitializeRolesAndAdminUser(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

            // Create Admin role if it doesn't exist
            string adminRoleName = "Admin";
            if (!await roleManager.RoleExistsAsync(adminRoleName))
            {
                var role = new IdentityRole(adminRoleName);
                await roleManager.CreateAsync(role);
                
                Console.WriteLine($"Created role: {adminRoleName}");
            }

            // Create admin user if it doesn't exist
            string adminEmail = "marc.coxall@ninjatec.co.uk";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            
            if (adminUser == null)
            {
                adminUser = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true // Pre-confirm email for admin
                };
                
                // Generate a complex password for initial setup
                string adminPassword = GenerateSecurePassword();
                var result = await userManager.CreateAsync(adminUser, adminPassword);
                
                if (result.Succeeded)
                {
                    Console.WriteLine($"Created admin user: {adminEmail}");
                    Console.WriteLine($"Admin initial password: {adminPassword}");
                    Console.WriteLine("Please change this password immediately after first login.");
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new Exception($"Failed to create admin user. Errors: {errors}");
                }
            }

            // Ensure the user is in the Admin role
            if (!await userManager.IsInRoleAsync(adminUser, adminRoleName))
            {
                await userManager.AddToRoleAsync(adminUser, adminRoleName);
                Console.WriteLine($"Added user {adminEmail} to {adminRoleName} role");
            }
        }

        // Generate a secure password for initial admin setup
        private static string GenerateSecurePassword()
        {
            // Define character sets for password complexity
            const string uppercaseChars = "ABCDEFGHJKLMNOPQRSTUVWXYZ";
            const string lowercaseChars = "abcdefghijkmnopqrstuvwxyz";
            const string digitChars = "0123456789";
            const string specialChars = "!@#$%^&*()_-+=<>?";
            
            var random = new Random();
            var password = new List<char>();
            
            // Ensure at least one character from each required set
            password.Add(uppercaseChars[random.Next(uppercaseChars.Length)]);
            password.Add(lowercaseChars[random.Next(lowercaseChars.Length)]);
            password.Add(digitChars[random.Next(digitChars.Length)]);
            password.Add(specialChars[random.Next(specialChars.Length)]);
            
            // Add additional random characters to reach desired length
            const int passwordLength = 16;
            var allChars = uppercaseChars + lowercaseChars + digitChars + specialChars;
            
            for (int i = password.Count; i < passwordLength; i++)
            {
                password.Add(allChars[random.Next(allChars.Length)]);
            }
            
            // Shuffle the password characters
            for (int i = 0; i < password.Count; i++)
            {
                int swapIndex = random.Next(password.Count);
                (password[i], password[swapIndex]) = (password[swapIndex], password[i]);
            }
            
            return new string(password.ToArray());
        }

        private static async Task SeedInitialVersion(ApplicationDbContext context)
        {
            // Check if any versions exist
            if (!await context.Versions.AnyAsync())
            {
                // Create initial version
                var initialVersion = new AppVersion
                {
                    Major = 1,
                    Minor = 0,
                    Patch = 0,
                    BuildNumber = 1,
                    Description = "Initial Release",
                    ReleaseDate = DateTime.UtcNow,
                    IsCurrent = true,
                    GitCommitHash = null,
                    ReleaseNotes = "First version of Workout Tracker Web application."
                };
                
                context.Versions.Add(initialVersion);
                await context.SaveChangesAsync();
                
                Console.WriteLine("Created initial application version: 1.0.0.1");
            }
        }

        private static async Task SeedExerciseTypes(WorkoutTrackerWebContext context)
        {
            if (await context.ExerciseType.AnyAsync())
            {
                return; // Already seeded
            }

            var exerciseTypes = new List<ExerciseType>
            {
                // Compound exercises
                new ExerciseType { Name = "Squat", Description = "A compound lower body exercise that engages the quadriceps, hamstrings, and glutes." },
                new ExerciseType { Name = "Deadlift", Description = "A compound exercise that works the entire posterior chain, focusing on the lower back, glutes, and hamstrings." },
                new ExerciseType { Name = "Bench Press", Description = "A compound upper body exercise primarily targeting the chest, shoulders, and triceps." },
                new ExerciseType { Name = "Overhead Press", Description = "A compound upper body exercise focusing on the shoulders, triceps, and upper chest." },
                new ExerciseType { Name = "Pull-up", Description = "A compound upper body exercise that targets the back, biceps, and shoulders." },
                new ExerciseType { Name = "Barbell Row", Description = "A compound back exercise that also engages the biceps and rear deltoids." },
                new ExerciseType { Name = "Dip", Description = "A compound exercise targeting the chest, triceps, and shoulders." },
                
                // Upper body isolation
                new ExerciseType { Name = "Bicep Curl", Description = "An isolation exercise targeting the biceps." },
                new ExerciseType { Name = "Tricep Extension", Description = "An isolation exercise focusing on the triceps." },
                new ExerciseType { Name = "Lateral Raise", Description = "An isolation exercise for the side deltoids." },
                new ExerciseType { Name = "Face Pull", Description = "An exercise targeting the rear deltoids and upper back." },
                
                // Lower body isolation
                new ExerciseType { Name = "Leg Extension", Description = "An isolation exercise targeting the quadriceps." },
                new ExerciseType { Name = "Leg Curl", Description = "An isolation exercise focusing on the hamstrings." },
                new ExerciseType { Name = "Calf Raise", Description = "An isolation exercise for the calf muscles." },
                
                // Core exercises
                new ExerciseType { Name = "Plank", Description = "A static exercise that strengthens the core, shoulders, and glutes." },
                new ExerciseType { Name = "Russian Twist", Description = "A rotational exercise targeting the obliques and abdominal muscles." },
                new ExerciseType { Name = "Leg Raise", Description = "A core exercise focusing on the lower abdominals." },
                
                // Cardio
                new ExerciseType { Name = "Running", Description = "A cardiovascular exercise performed at various intensities." },
                new ExerciseType { Name = "Cycling", Description = "A low-impact cardiovascular exercise using a bicycle or stationary bike." },
                new ExerciseType { Name = "Rowing", Description = "A full-body cardiovascular exercise using a rowing machine." }
            };

            await context.ExerciseType.AddRangeAsync(exerciseTypes);
            await context.SaveChangesAsync();
        }

        private static async Task SeedSetTypes(WorkoutTrackerWebContext context)
        {
            if (await context.Settype.AnyAsync())
            {
                return; // Already seeded
            }

            var setTypes = new List<Settype>
            {
                new Settype { Name = "Warm-up", Description = "A lighter set to prepare muscles and joints for heavier work" },
                new Settype { Name = "Working", Description = "Main training set with challenging weight" },
                new Settype { Name = "Drop", Description = "Set performed immediately after a working set with reduced weight" },
                new Settype { Name = "Failure", Description = "Set performed until muscular failure" },
                new Settype { Name = "AMRAP", Description = "As Many Reps As Possible" },
                new Settype { Name = "Pyramid", Description = "Series of sets with increasing or decreasing weight" },
                new Settype { Name = "Super", Description = "Two exercises performed back-to-back without rest" }
            };

            await context.Settype.AddRangeAsync(setTypes);
            await context.SaveChangesAsync();
        }

        private static async Task SeedHelpContent(WorkoutTrackerWebContext context)
        {
            if (await context.HelpCategory.AnyAsync())
            {
                return; // Already seeded
            }

            // Create help categories
            var gettingStarted = new HelpCategory
            {
                Name = "Getting Started",
                Description = "Basic information to help you begin using Workout Tracker",
                DisplayOrder = 1
            };

            var workouts = new HelpCategory
            {
                Name = "Workouts",
                Description = "Information about creating and managing your workout sessions",
                DisplayOrder = 2
            };

            var exercises = new HelpCategory
            {
                Name = "Exercises",
                Description = "Guidance on exercise types and tracking progress",
                DisplayOrder = 3
            };

            var reporting = new HelpCategory
            {
                Name = "Reports & Analytics",
                Description = "Understanding your workout data and progress",
                DisplayOrder = 4
            };

            var account = new HelpCategory
            {
                Name = "Account & Settings",
                Description = "Managing your account, preferences, and security",
                DisplayOrder = 5
            };

            // Add categories to context
            await context.HelpCategory.AddRangeAsync(gettingStarted, workouts, exercises, reporting, account);
            await context.SaveChangesAsync();

            // Create articles
            var articles = new List<HelpArticle>
            {
                // Getting Started articles
                new HelpArticle
                {
                    Title = "Welcome to Workout Tracker",
                    ShortDescription = "An introduction to the Workout Tracker application",
                    Content = "<h2>Welcome to Workout Tracker</h2><p>Workout Tracker is a comprehensive tool designed to help you monitor, track, and improve your fitness journey. Whether you're a beginner or an experienced athlete, our platform provides the tools you need to record workouts, track progress, and achieve your fitness goals.</p><h3>Key Features</h3><ul><li>Track workout sessions with detailed exercise information</li><li>Monitor progress with comprehensive reports and analytics</li><li>Calculate one-rep max and other fitness metrics</li><li>Organize exercises by type and track performance over time</li></ul><p>Get started by <a href='/Identity/Account/Register'>creating an account</a> or <a href='/Identity/Account/Login'>logging in</a> if you already have one.</p>",
                    HelpCategoryId = gettingStarted.Id,
                    IsFeatured = true,
                    ViewCount = 0,
                    CreatedDate = DateTime.Now.AddDays(-30),
                    LastModifiedDate = DateTime.Now,
                    IsPrintable = true,
                    Tags = "welcome,introduction,overview"
                },
                new HelpArticle
                {
                    Title = "Creating Your First Workout",
                    ShortDescription = "Learn how to create your first workout session",
                    Content = "<h2>Creating Your First Workout</h2><p>Recording your workouts is easy with Workout Tracker. Follow these steps to create your first workout session:</p><ol><li>Navigate to the <strong>Workouts</strong> page from the main navigation</li><li>Click the <strong>Create New</strong> button</li><li>Enter a name for your workout session (e.g., \"Monday Chest Day\")</li><li>Add the date and time of your workout</li><li>Click <strong>Create</strong> to save your workout session</li></ol><p>Once you've created a session, you can add exercises to it by clicking on the session and then selecting <strong>Add Set</strong>.</p><h3>Adding Sets and Reps</h3><p>For each exercise you perform:</p><ol><li>Select the exercise type from the dropdown</li><li>Choose the set type (e.g., warm-up, working set)</li><li>Enter the weight used</li><li>Add the number of reps completed</li><li>Click <strong>Create</strong> to save the set</li></ol><p>You can continue adding sets for different exercises throughout your workout session.</p>",
                    HelpCategoryId = gettingStarted.Id,
                    IsFeatured = true,
                    ViewCount = 0,
                    CreatedDate = DateTime.Now.AddDays(-28),
                    LastModifiedDate = DateTime.Now,
                    IsPrintable = true,
                    Tags = "getting started,workouts,tutorial"
                },
                
                // Workouts articles
                new HelpArticle
                {
                    Title = "Managing Workout Sessions",
                    ShortDescription = "Learn how to edit, delete, and organize your workout sessions",
                    Content = "<h2>Managing Workout Sessions</h2><p>Workout Tracker makes it easy to keep your workout sessions organized. Here's how to manage your sessions effectively:</p><h3>Viewing Your Sessions</h3><p>All your workout sessions are listed on the <strong>Workouts</strong> page. You can sort and filter this list to find specific sessions.</p><h3>Editing a Session</h3><p>To modify the details of a workout session:</p><ol><li>Find the session in your list</li><li>Click the <strong>Edit</strong> link next to the session</li><li>Update the name, date, or other details</li><li>Click <strong>Save</strong> to confirm your changes</li></ol><h3>Deleting a Session</h3><p>If you need to remove a workout session:</p><ol><li>Locate the session in your list</li><li>Click the <strong>Delete</strong> link</li><li>Confirm the deletion when prompted</li></ol><p><strong>Note:</strong> Deletion is permanent and will remove all sets and reps associated with that session.</p><h3>Session Details</h3><p>To view the complete details of a session, click on its name in the list. From the details page, you can:</p><ul><li>Add new sets to the session</li><li>Edit existing sets</li><li>Record reps for each set</li><li>Track performance metrics</li></ul>",
                    HelpCategoryId = workouts.Id,
                    IsFeatured = false,
                    ViewCount = 0,
                    CreatedDate = DateTime.Now.AddDays(-25),
                    LastModifiedDate = DateTime.Now,
                    IsPrintable = true,
                    Tags = "workouts,sessions,management"
                },
                
                // Exercises articles
                new HelpArticle
                {
                    Title = "Understanding Exercise Types",
                    ShortDescription = "Learn about different exercise types and how to use them",
                    Content = "<h2>Understanding Exercise Types</h2><p>Workout Tracker organizes exercises by type to help you track progress across similar movements. Here's what you need to know about exercise types:</p><h3>What Are Exercise Types?</h3><p>Exercise types categorize different movements based on the primary muscles they target or the movement pattern they follow. Examples include:</p><ul><li>Squat</li><li>Bench Press</li><li>Deadlift</li><li>Overhead Press</li><li>Pull-up</li><li>Bicep Curl</li></ul><h3>Adding Custom Exercise Types</h3><p>While Workout Tracker comes with many common exercises pre-loaded, you can add your own:</p><ol><li>Navigate to the <strong>Exercise Types</strong> section</li><li>Click <strong>Create New</strong></li><li>Enter a name and optional description</li><li>Click <strong>Create</strong> to add your custom exercise</li></ol><h3>Using Exercise Types in Workouts</h3><p>When recording sets in your workout:</p><ol><li>Select the appropriate exercise type from the dropdown</li><li>This ensures your progress is tracked consistently for each exercise</li></ol><h3>Exercise Type History</h3><p>Workout Tracker uses your exercise type selections to generate progress reports, showing improvements in strength, endurance, and other metrics over time.</p>",
                    HelpCategoryId = exercises.Id,
                    IsFeatured = false,
                    ViewCount = 0,
                    CreatedDate = DateTime.Now.AddDays(-20),
                    LastModifiedDate = DateTime.Now,
                    IsPrintable = true,
                    Tags = "exercises,types,categories"
                },
                
                // Reporting articles
                new HelpArticle
                {
                    Title = "Using the Reports Dashboard",
                    ShortDescription = "Learn how to use reports to track your progress",
                    Content = "<h2>Using the Reports Dashboard</h2><p>Workout Tracker's reporting features help you visualize progress and identify trends in your fitness journey.</p><h3>Accessing Reports</h3><p>To view your personalized reports:</p><ol><li>Click on the <strong>Reports</strong> link in the main navigation</li><li>The dashboard displays an overview of your training history and progress</li></ol><h3>Understanding Your Reports</h3><p>The reports dashboard includes several key sections:</p><h4>Personal Records (PRs)</h4><p>A table showing your maximum weights lifted for each exercise type, highlighting your personal bests.</p><h4>Weight Progress</h4><p>Charts showing how the weight you're lifting has changed over time for different exercises, helping you visualize strength gains.</p><h4>Rep Success Rate</h4><p>A pie chart showing the percentage of successful versus failed reps, giving you insight into your training intensity.</p><h3>Filtering Reports</h3><p>You can customize your reports by:</p><ul><li>Selecting specific date ranges</li><li>Focusing on particular exercise types</li><li>Choosing different metrics to display</li></ul><p>Use these filters to analyze specific aspects of your training or to focus on particular goals.</p><h3>Using Reports to Improve</h3><p>Your reports can help identify:</p><ul><li>Exercises where you're making good progress</li><li>Areas that might need more attention</li><li>Optimal training volumes and intensities</li><li>Patterns in your training that lead to better results</li></ul>",
                    HelpCategoryId = reporting.Id,
                    IsFeatured = true,
                    ViewCount = 0,
                    CreatedDate = DateTime.Now.AddDays(-15),
                    LastModifiedDate = DateTime.Now,
                    IsPrintable = true,
                    Tags = "reports,analytics,progress,tracking"
                },
                
                // Account & Settings articles
                new HelpArticle
                {
                    Title = "Managing Your Account",
                    ShortDescription = "Learn how to update your profile and security settings",
                    Content = "<h2>Managing Your Account</h2><p>Keeping your account information up to date ensures you get the best experience with Workout Tracker.</p><h3>Updating Your Profile</h3><p>To edit your personal information:</p><ol><li>Click on your username in the top-right corner</li><li>Select <strong>Profile</strong> from the dropdown menu</li><li>Update your details as needed</li><li>Click <strong>Save</strong> to confirm changes</li></ol><h3>Changing Your Password</h3><p>For security reasons, you should change your password periodically:</p><ol><li>Go to your profile page</li><li>Select the <strong>Password</strong> tab</li><li>Enter your current password</li><li>Create and confirm your new password</li><li>Click <strong>Update Password</strong></li></ol><h3>Email Preferences</h3><p>You can control what notifications you receive:</p><ol><li>Go to your profile page</li><li>Select the <strong>Email Preferences</strong> tab</li><li>Toggle the options according to your preferences</li><li>Click <strong>Save Changes</strong></li></ol><h3>Account Security</h3><p>Workout Tracker implements several security measures to protect your data:</p><ul><li>Secure password storage</li><li>Email verification for important changes</li><li>Session timeouts after periods of inactivity</li></ul><p>If you notice any suspicious activity on your account, please change your password immediately and <a href='/Feedback/Create'>contact support</a>.</p>",
                    HelpCategoryId = account.Id,
                    IsFeatured = false,
                    ViewCount = 0,
                    CreatedDate = DateTime.Now.AddDays(-10),
                    LastModifiedDate = DateTime.Now,
                    IsPrintable = true,
                    Tags = "account,profile,security,settings"
                }
            };

            await context.HelpArticle.AddRangeAsync(articles);
            await context.SaveChangesAsync();

            // Set up related articles relationships
            var welcomeArticle = await context.HelpArticle.FirstAsync(a => a.Title == "Welcome to Workout Tracker");
            var firstWorkoutArticle = await context.HelpArticle.FirstAsync(a => a.Title == "Creating Your First Workout");
            var exerciseTypesArticle = await context.HelpArticle.FirstAsync(a => a.Title == "Understanding Exercise Types");

            // Add related articles
            welcomeArticle.RelatedArticles = new List<HelpArticle> { firstWorkoutArticle };
            firstWorkoutArticle.RelatedArticles = new List<HelpArticle> { welcomeArticle, exerciseTypesArticle };
            
            await context.SaveChangesAsync();
        }

        private static async Task SeedGlossaryTerms(WorkoutTrackerWebContext context)
        {
            if (await context.GlossaryTerm.AnyAsync())
            {
                return; // Already seeded
            }

            var glossaryTerms = new List<GlossaryTerm>
            {
                new GlossaryTerm {
                    Term = "1RM (One-Rep Max)",
                    Definition = "The maximum amount of weight that can be lifted for a single repetition of a given exercise.",
                    Category = "Training Metrics",
                    Example = "A 1RM of 225 pounds on the bench press means you can lift that weight exactly once with proper form."
                },
                new GlossaryTerm {
                    Term = "AMRAP",
                    Definition = "As Many Reps As Possible. A training protocol where you perform as many repetitions as you can with a given weight.",
                    Category = "Training Protocols",
                    Example = "During the final set, perform AMRAP with 135 pounds on the squat."
                },
                new GlossaryTerm {
                    Term = "Compound Exercise",
                    Definition = "Movements that work multiple muscle groups simultaneously.",
                    Category = "Exercise Types",
                    Example = "Squats, deadlifts, bench press, and pull-ups are all compound exercises."
                },
                new GlossaryTerm {
                    Term = "Drop Set",
                    Definition = "A technique where you perform a set to failure, then immediately reduce the weight and continue with more repetitions without rest.",
                    Category = "Training Techniques",
                    Example = "Perform 8 reps with 30kg, then immediately drop to 25kg and perform as many reps as possible."
                },
                new GlossaryTerm {
                    Term = "Hypertrophy",
                    Definition = "The enlargement of muscle fibers, resulting in increased muscle size.",
                    Category = "Physiology",
                    Example = "Training in the 8-12 rep range is often recommended for optimal hypertrophy."
                },
                new GlossaryTerm {
                    Term = "Isolation Exercise",
                    Definition = "Movements that primarily work a single muscle group.",
                    Category = "Exercise Types",
                    Example = "Bicep curls, tricep extensions, and leg extensions are all isolation exercises."
                },
                new GlossaryTerm {
                    Term = "Progressive Overload",
                    Definition = "The gradual increase in stress placed on the body during exercise training to stimulate continued improvement.",
                    Category = "Training Principles",
                    Example = "Adding 5 pounds to your squat each week is an example of progressive overload."
                },
                new GlossaryTerm {
                    Term = "Rep (Repetition)",
                    Definition = "A single execution of an exercise movement.",
                    Category = "Training Basics",
                    Example = "Performing 10 reps means completing the full movement 10 times."
                },
                new GlossaryTerm {
                    Term = "RPE (Rate of Perceived Exertion)",
                    Definition = "A subjective scale from 1-10 that measures how difficult a set feels.",
                    Category = "Training Metrics",
                    Example = "An RPE of 9 means you could have performed one more rep at most."
                },
                new GlossaryTerm {
                    Term = "Set",
                    Definition = "A group of consecutive repetitions of an exercise.",
                    Category = "Training Basics",
                    Example = "3 sets of 10 reps means performing 10 repetitions, resting, then repeating this two more times."
                },
                new GlossaryTerm {
                    Term = "Superset",
                    Definition = "A training method where two different exercises are performed back-to-back with no rest between them.",
                    Category = "Training Techniques",
                    Example = "Performing bicep curls immediately followed by tricep extensions is a common superset."
                },
                new GlossaryTerm {
                    Term = "Warm-up Set",
                    Definition = "A set performed with lighter weight before the main working sets to prepare the body for heavier loads.",
                    Category = "Training Basics",
                    Example = "Before squatting 225 pounds, perform warm-up sets with 135 and 185 pounds."
                }
            };

            // Add basic terms
            await context.GlossaryTerm.AddRangeAsync(glossaryTerms);
            await context.SaveChangesAsync();

            // Set up related terms relationships
            var oneRmTerm = await context.GlossaryTerm.FirstAsync(t => t.Term == "1RM (One-Rep Max)");
            var repTerm = await context.GlossaryTerm.FirstAsync(t => t.Term == "Rep (Repetition)");
            var setTerm = await context.GlossaryTerm.FirstAsync(t => t.Term == "Set");
            var compoundTerm = await context.GlossaryTerm.FirstAsync(t => t.Term == "Compound Exercise");
            var isolationTerm = await context.GlossaryTerm.FirstAsync(t => t.Term == "Isolation Exercise");
            var dropSetTerm = await context.GlossaryTerm.FirstAsync(t => t.Term == "Drop Set");
            var supersetTerm = await context.GlossaryTerm.FirstAsync(t => t.Term == "Superset");
            var amrapTerm = await context.GlossaryTerm.FirstAsync(t => t.Term == "AMRAP");

            // Add related terms
            oneRmTerm.RelatedTerms = new List<GlossaryTerm> { repTerm, setTerm };
            repTerm.RelatedTerms = new List<GlossaryTerm> { oneRmTerm, setTerm };
            setTerm.RelatedTerms = new List<GlossaryTerm> { repTerm, dropSetTerm, supersetTerm };
            compoundTerm.RelatedTerms = new List<GlossaryTerm> { isolationTerm };
            isolationTerm.RelatedTerms = new List<GlossaryTerm> { compoundTerm };
            dropSetTerm.RelatedTerms = new List<GlossaryTerm> { setTerm, supersetTerm };
            supersetTerm.RelatedTerms = new List<GlossaryTerm> { setTerm, dropSetTerm };
            amrapTerm.RelatedTerms = new List<GlossaryTerm> { repTerm, setTerm };

            await context.SaveChangesAsync();
        }
    }
}