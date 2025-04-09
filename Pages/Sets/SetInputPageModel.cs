using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace WorkoutTrackerWeb.Pages.Sets
{
    public class SetInputPageModel : PageModel
    {
        public SelectList SettypeNameSL { get; set; }
        public SelectList ExerciseNameSL { get; set; }

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