using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerweb.Data;

namespace WorkoutTrackerWeb.Pages.Sets
{
    public class IndexModel : PageModel
    {
        private readonly WorkoutTrackerweb.Data.WorkoutTrackerWebContext _context;

        public IndexModel(WorkoutTrackerweb.Data.WorkoutTrackerWebContext context)
        {
            _context = context;
        }

        public IList<Set> Set { get;set; } = default!;
        public string SessionSort { get; set; }
        public string ExerciseSort { get; set; }
        public string SetTypeSort { get; set; }
        public string DescriptionSort { get; set; }
        public string NotesSort { get; set; }
        public string NumberRepsSort { get; set; }
        public string WeightSort { get; set; }
        public string CurrentSort { get; set; }
        public string CurrentFilter { get; set; }

        public async Task OnGetAsync(string sortOrder)
        {
            // Set up sorting parameters
            CurrentSort = sortOrder;
            SessionSort = string.IsNullOrEmpty(sortOrder) ? "session_desc" : "";
            ExerciseSort = sortOrder == "exercise" ? "exercise_desc" : "exercise";
            SetTypeSort = sortOrder == "settype" ? "settype_desc" : "settype";
            DescriptionSort = sortOrder == "description" ? "description_desc" : "description";
            NotesSort = sortOrder == "notes" ? "notes_desc" : "notes";
            NumberRepsSort = sortOrder == "reps" ? "reps_desc" : "reps";
            WeightSort = sortOrder == "weight" ? "weight_desc" : "weight";

            // Get the data
            IQueryable<Set> setsIQ = _context.Set
                .Include(s => s.Session)
                .Include(s => s.ExerciseType)
                .Include(s => s.Settype);

            // Apply sorting based on selected column
            switch (sortOrder)
            {
                case "session_desc":
                    setsIQ = setsIQ.OrderByDescending(s => s.Session.Name);
                    break;
                case "exercise":
                    setsIQ = setsIQ.OrderBy(s => s.ExerciseType.Name);
                    break;
                case "exercise_desc":
                    setsIQ = setsIQ.OrderByDescending(s => s.ExerciseType.Name);
                    break;
                case "settype":
                    setsIQ = setsIQ.OrderBy(s => s.Settype.Name);
                    break;
                case "settype_desc":
                    setsIQ = setsIQ.OrderByDescending(s => s.Settype.Name);
                    break;
                case "description":
                    setsIQ = setsIQ.OrderBy(s => s.Description);
                    break;
                case "description_desc":
                    setsIQ = setsIQ.OrderByDescending(s => s.Description);
                    break;
                case "notes":
                    setsIQ = setsIQ.OrderBy(s => s.Notes);
                    break;
                case "notes_desc":
                    setsIQ = setsIQ.OrderByDescending(s => s.Notes);
                    break;
                case "reps":
                    setsIQ = setsIQ.OrderBy(s => s.NumberReps);
                    break;
                case "reps_desc":
                    setsIQ = setsIQ.OrderByDescending(s => s.NumberReps);
                    break;
                case "weight":
                    setsIQ = setsIQ.OrderBy(s => s.Weight);
                    break;
                case "weight_desc":
                    setsIQ = setsIQ.OrderByDescending(s => s.Weight);
                    break;
                default:
                    setsIQ = setsIQ.OrderBy(s => s.Session.Name);
                    break;
            }

            Set = await setsIQ.ToListAsync();
        }
        
        public async Task<IActionResult> OnGetDuplicateAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Find the set to duplicate with related entities
            var setToDuplicate = await _context.Set
                .Include(s => s.ExerciseType)
                .Include(s => s.Session)
                .Include(s => s.Settype)
                .FirstOrDefaultAsync(m => m.SetId == id);

            if (setToDuplicate == null)
            {
                return NotFound();
            }

            // Create a new set with copied properties
            var newSet = new Set
            {
                Description = setToDuplicate.Description + " (Copy)",
                Notes = setToDuplicate.Notes,
                SettypeId = setToDuplicate.SettypeId,
                ExerciseTypeId = setToDuplicate.ExerciseTypeId,
                SessionId = setToDuplicate.SessionId,
                NumberReps = setToDuplicate.NumberReps,
                Weight = setToDuplicate.Weight
                // SetId is not copied as it's the primary key and will be generated by the database
            };

            // Add the new set to the database
            _context.Set.Add(newSet);
            await _context.SaveChangesAsync();

            // Automatically create Rep records based on NumberReps
            if (newSet.NumberReps > 0)
            {
                for (int i = 0; i < newSet.NumberReps; i++)
                {
                    var rep = new Rep 
                    { 
                        SetsSetId = newSet.SetId,
                        repnumber = i + 1,
                        weight = newSet.Weight, // Use the set's weight
                        success = true
                    };
                    _context.Rep.Add(rep);
                }
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
