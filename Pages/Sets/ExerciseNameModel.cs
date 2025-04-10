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
    public class ExerciseTypePageModel : PageModel
    {
        public SelectList ExerciseTypeSL { get; set; }

        // Method to populate exercise types dropdown
        // No user filtering since exercise types are shared across all users
        public async Task PopulateExerciseTypeDropDownListAsync(
            WorkoutTrackerweb.Data.WorkoutTrackerWebContext context,
            object selectedExerciseType = null)
        {
            var exerciseTypeQuery = from et in context.ExerciseType
                                  orderby et.Name
                                  select et;

            ExerciseTypeSL = new SelectList(await exerciseTypeQuery.AsNoTracking().ToListAsync(), 
                nameof(ExerciseType.ExerciseTypeId), 
                nameof(ExerciseType.Name),
                selectedExerciseType);
        }
    }
}