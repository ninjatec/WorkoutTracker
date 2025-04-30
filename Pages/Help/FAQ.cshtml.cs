using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;

namespace WorkoutTrackerWeb.Pages.Help
{
    [OutputCache(PolicyName = "StaticContent")]
    public class FAQModel : PageModel
    {
        public Dictionary<string, List<(string question, string answer)>> FaqCategories { get; private set; }

        public void OnGet()
        {
            FaqCategories = new Dictionary<string, List<(string question, string answer)>>
            {
                ["Account Management"] = new List<(string question, string answer)>
                {
                    ("How do I create an account?", 
                     "To create an account, click the <strong>Register</strong> link in the top right corner of the page. " +
                     "Fill in your email address, choose a password, and click <strong>Register</strong>. " +
                     "You'll receive a confirmation email to verify your account before you can log in."),
                    
                    ("How do I reset my password?", 
                     "If you've forgotten your password, click the <strong>Login</strong> link, then click " +
                     "<strong>Forgot your password?</strong>. Enter your email address, and we'll send you " +
                     "a link to reset your password."),
                    
                    ("Can I change my email address?", 
                     "Yes, once logged in, go to your account settings by clicking on your username in the top right corner " +
                     "and selecting <strong>Profile</strong>. From there, you can update your email address and other profile information."),
                    
                    ("How do I delete my account?", 
                     "To delete your account, go to your profile settings, scroll to the bottom, and click " +
                     "<strong>Delete Account</strong>. Please note that this action is permanent and will delete all your data.")
                },

                ["Workout Sessions"] = new List<(string question, string answer)>
                {
                    ("How do I create a new workout session?", 
                     "To create a new workout session, click on <strong>Sessions</strong> in the main navigation, " +
                     "then click the <strong>Create New Session</strong> button. Give your session a name, " +
                     "set the date and time, and click <strong>Create</strong>."),
                    
                    ("Can I edit a workout session after creating it?", 
                     "Yes, you can edit a session by viewing it and clicking the <strong>Edit</strong> button. " +
                     "From there, you can change the session name and date."),
                    
                    ("How do I delete a workout session?", 
                     "To delete a session, go to the session details page and click the <strong>Delete</strong> button. " +
                     "You'll be asked to confirm this action, as deleting a session will also delete all sets and reps associated with it."),
                    
                    ("Can I duplicate a previous workout session?", 
                     "Yes, you can clone a previous session by viewing it and clicking <strong>Clone Session</strong>. " +
                     "This will create a new session with the same sets but with today's date.")
                },

                ["Sets and Reps"] = new List<(string question, string answer)>
                {
                    ("How do I add sets to a workout session?", 
                     "Once you've created a session, view the session details and click <strong>Add Set</strong>. " +
                     "Select an exercise type, specify the set type (e.g., regular, warm-up), enter the weight, " +
                     "and click <strong>Create</strong>."),
                    
                    ("How do I record repetitions (reps) for a set?", 
                     "After creating a set, click on the set to view its details, then click <strong>Add Rep</strong>. " +
                     "For each rep, you can mark whether it was successful and record the weight (which defaults to the set weight)."),
                    
                    ("What's the difference between sets and reps?", 
                     "A <strong>Set</strong> is a group of repetitions of an exercise performed consecutively without rest. " +
                     "A <strong>Rep</strong> (repetition) is a single execution of an exercise. For example, doing 3 sets of 10 reps " +
                     "means you'll do the exercise 10 times, take a break, do it 10 more times, take another break, and do it 10 more times."),
                    
                    ("How do I track failed reps?", 
                     "When adding or editing a rep, uncheck the <strong>Success</strong> checkbox to mark it as failed. " +
                     "This helps track your progress and identify exercises where you might need to adjust the weight.")
                },

                ["Exercise Types"] = new List<(string question, string answer)>
                {
                    ("How do I add a new type of exercise?", 
                     "To add a new exercise type, go to <strong>Exercise Types</strong> in the main navigation and click " +
                     "<strong>Create New</strong>. Enter a name for the exercise (e.g., 'Bench Press' or 'Squats') and click <strong>Create</strong>."),
                    
                    ("Can I edit or delete an exercise type?", 
                     "Yes, on the Exercise Types page, find the exercise you want to modify and click <strong>Edit</strong> or <strong>Delete</strong>. " +
                     "Note that deleting an exercise type will not delete any sets that use it, but those sets will no longer show the exercise name."),
                    
                    ("How are exercise types different from set types?", 
                     "Exercise types describe <em>what</em> exercise you're doing (e.g., 'Bench Press'), while set types describe " +
                     "<em>how</em> you're doing it (e.g., 'Warm-up', 'Regular', 'Drop Set'). This separation allows for better tracking and reporting."),
                    
                    ("How do I find exercises I've done before?", 
                     "The Exercise Types list shows all exercises you've created. When creating a new set, " +
                     "you can select from this list to ensure consistent naming and better reporting.")
                },

                ["Reports and Progress Tracking"] = new List<(string question, string answer)>
                {
                    ("How do I view my progress over time?", 
                     "Go to the <strong>Reports</strong> section to see visualizations of your workout data. " +
                     "You'll find charts showing your weight progression for different exercises, success rates, " +
                     "and personal records (PRs)."),
                    
                    ("What is a Personal Record (PR)?", 
                     "A Personal Record (PR) is the maximum weight you've successfully lifted for a particular exercise. " +
                     "The Reports page shows your current PRs for each exercise type you've performed."),
                    
                    ("How is the One Rep Max calculated?", 
                     "The One Rep Max (1RM) is an estimate of the maximum weight you could lift for one repetition. " +
                     "It's calculated based on the weight and reps you've recorded using seven different scientific formulas. " +
                     "The app automatically excludes warm-up sets from these calculations for accuracy."),
                    
                    ("How do I track my success rate?", 
                     "The Reports section includes a pie chart showing your successful vs. failed reps. " +
                     "This helps you identify if you're pushing yourself too hard (many failed reps) or " +
                     "not challenging yourself enough (very few failed reps).")
                },

                ["Troubleshooting"] = new List<(string question, string answer)>
                {
                    ("Why can't I see my workout data?", 
                     "Make sure you're logged in with the correct account. If you're still having issues, " +
                     "try refreshing the page. If the problem persists, contact support through the Feedback page."),
                    
                    ("The app is running slowly. What can I do?", 
                     "Try clearing your browser cache and cookies. If you're using a mobile device, " +
                     "ensure you have a stable internet connection and sufficient storage space."),
                    
                    ("I found a bug. How do I report it?", 
                     "Please use the <strong>Feedback</strong> link in the navigation to report bugs. " +
                     "Include as much detail as possible, such as what you were doing when the bug occurred, " +
                     "what device and browser you were using, and any error messages you saw."),
                    
                    ("How do I request a new feature?", 
                     "Feature requests can also be submitted through the <strong>Feedback</strong> page. " +
                     "Select 'Feature Request' as the feedback type and describe the feature you'd like to see added.")
                },

                ["Coaching"] = new List<(string question, string answer)>
                {
                    ("How do I become a coach?", 
                     "To become a coach, you need to have an active account. Go to your profile settings and look for the " +
                     "'Become a Coach' option. Once approved, you'll have access to the coaching dashboard and features."),
                    
                    ("How do I find and connect with a coach?", 
                     "You can find coaches through the 'Find a Coach' section. Browse available coaches, view their profiles, " +
                     "and send a connection request. Once a coach accepts your request, they can start programming workouts for you."),
                    
                    ("As a coach, how do I manage my clients?", 
                     "Coaches can manage their clients through the coach dashboard. Here you can view your client roster, " +
                     "create workout templates, assign workouts, and track client progress. Use the client management tools " +
                     "to group clients and manage their training programs."),
                    
                    ("How do workout templates work for coaches?", 
                     "Coaches can create workout templates and assign them to individual clients or groups. Templates can be " +
                     "scheduled as one-time or recurring workouts. Clients will see their assigned workouts in their dashboard " +
                     "and receive notifications for upcoming sessions."),
                    
                    ("Can I share my workout data with others?", 
                     "Yes, you can share your workout data using the sharing feature. Go to your workout history and click " +
                     "the 'Share' button to generate a secure sharing link. You can set an expiration date and control what " +
                     "data is visible to others.")
                }
            };
        }
    }
}