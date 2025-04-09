using WorkoutTrackerWeb.Data;
using WorkoutTrackerweb.Data;
using WorkoutTrackerWeb.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using WorkoutTrackerWeb.Services;
using System.Threading.Tasks;

namespace WorkoutTrackerWeb.Pages.Sets
{
    public class ExerciseNamePageModel : PageModel
    {
        public SelectList ExerciseNameSL { get; set; }

        // Async method that filters exercises by current user
        public async Task PopulateExerciseNameDropDownListAsync(
            WorkoutTrackerweb.Data.WorkoutTrackerWebContext _context,
            UserService userService,
            object selectedExercise = null)
        {
            // Get current user ID
            var currentUserId = await userService.GetCurrentUserIdAsync();

            // Filter exercises by sessions belonging to current user
            var exerciseQuery = from e in _context.Excercise
                             join s in _context.Session on e.SessionId equals s.SessionId
                             where s.UserId == currentUserId
                             orderby e.ExcerciseName
                             select e;

            ExerciseNameSL = new SelectList(await exerciseQuery.AsNoTracking().ToListAsync(), 
                nameof(Models.Excercise.ExcerciseId), 
                nameof(Models.Excercise.ExcerciseName),
                selectedExercise);
        }

        // Legacy method for backward compatibility
        public void PopulateExerciseNameDropDownList(
            WorkoutTrackerweb.Data.WorkoutTrackerWebContext _context, 
            object selectedExercise = null)
        {
            var exerciseQuery = from e in _context.Excercise
                            orderby e.ExcerciseName
                            select e;

            ExerciseNameSL = new SelectList(exerciseQuery.AsNoTracking(), 
                nameof(Models.Excercise.ExcerciseId), 
                nameof(Models.Excercise.ExcerciseName),
                selectedExercise);
        }
    }
}