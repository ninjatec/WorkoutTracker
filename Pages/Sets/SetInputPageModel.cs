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
        public SelectList ExerciseTypeSL { get; set; }
        public SelectList SessionNameSL { get; set; }

        public async Task PopulateDropDownListsAsync(
            WorkoutTrackerweb.Data.WorkoutTrackerWebContext context,
            UserService userService,
            object selectedExerciseType = null,
            object selectedSetType = null,
            object selectedSession = null)
        {
            // Get current user ID for filtering
            var currentUserId = await userService.GetCurrentUserIdAsync();

            // Get sessions for the current user only
            var sessionsQuery = context.Session
                .Where(s => s.UserId == currentUserId)
                .OrderBy(s => s.Name);
            
            // Get all exercise types (not filtered by user as they're shared)
            var exerciseTypesQuery = context.ExerciseType.OrderBy(e => e.Name);
            
            // Get all set types
            var setTypesQuery = context.Settype.OrderBy(s => s.Name);
            
            SessionNameSL = new SelectList(await sessionsQuery.ToListAsync(),
                "SessionId", "Name", selectedSession);
                
            ExerciseTypeSL = new SelectList(await exerciseTypesQuery.ToListAsync(),
                "ExerciseTypeId", "Name", selectedExerciseType);
                
            SettypeNameSL = new SelectList(await setTypesQuery.ToListAsync(),
                "SettypeId", "Name", selectedSetType);
        }
    }
}