using WorkoutTrackerWeb.Data;
using WorkoutTrackerweb.Data;
using WorkoutTrackerWeb.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Services;
using System.Linq;
using System.Threading.Tasks;

namespace WorkoutTrackerWeb.Pages.Sets
{
    public class SetInputPageModel : PageModel
    {
        public SelectList SettypeNameSL { get; set; }
        public SelectList ExerciseNameSL { get; set; }

        // Keep original method for SetType since it's not user-specific
        public void PopulateSettypeNameDropDownList(WorkoutTrackerweb.Data.WorkoutTrackerWebContext _context,
            object selectedSettype = null)
        {
            var settypesQuery = from s in _context.Settype
                               orderby s.Name
                               select s;

            SettypeNameSL = new SelectList(settypesQuery.AsNoTracking(),
                nameof(Settype.SettypeId),
                nameof(Settype.Name),
                selectedSettype);
        }

        // Add async version with user filtering for exercises
        public async Task PopulateExerciseNameDropDownListAsync(
            WorkoutTrackerweb.Data.WorkoutTrackerWebContext _context,
            UserService userService,
            object selectedExercise = null)
        {
            // Get current user ID
            var currentUserId = await userService.GetCurrentUserIdAsync();

            // Filter exercises by sessions belonging to current user
            var exercisesQuery = from e in _context.Excercise
                                join s in _context.Session on e.SessionId equals s.SessionId
                                where s.UserId == currentUserId
                                orderby e.ExcerciseName
                                select e;

            ExerciseNameSL = new SelectList(await exercisesQuery.AsNoTracking().ToListAsync(),
                nameof(Excercise.ExcerciseId),
                nameof(Excercise.ExcerciseName),
                selectedExercise);
        }

        // Legacy method for backward compatibility
        public void PopulateExerciseNameDropDownList(WorkoutTrackerweb.Data.WorkoutTrackerWebContext _context,
            object selectedExercise = null)
        {
            var exercisesQuery = from e in _context.Excercise
                                orderby e.ExcerciseName
                                select e;

            ExerciseNameSL = new SelectList(exercisesQuery.AsNoTracking(),
                nameof(Excercise.ExcerciseId),
                nameof(Excercise.ExcerciseName),
                selectedExercise);
        }
    }
}