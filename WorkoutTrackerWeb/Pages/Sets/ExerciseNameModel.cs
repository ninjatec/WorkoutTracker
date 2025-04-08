using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WorkoutTrackerWeb.Pages.Sets
{
    public class ExerciseNamePageModel : PageModel
    {
        public SelectList ExerciseNameSL { get; set; }

        public void PopulateExerciseNameDropDownList(WorkoutTrackerweb.Data.WorkoutTrackerWebContext _context, 
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