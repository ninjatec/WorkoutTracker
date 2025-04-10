using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerweb.Data;
using WorkoutTrackerWeb.Services;

namespace WorkoutTrackerWeb.Pages.Sets
{
    public class EditModel : SetInputPageModel
    {
        private readonly WorkoutTrackerweb.Data.WorkoutTrackerWebContext _context;
        private readonly UserService _userService;

        public EditModel(
            WorkoutTrackerweb.Data.WorkoutTrackerWebContext context,
            UserService userService)
        {
            _context = context;
            _userService = userService;
        }

        [BindProperty]
        public Set Set { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var set = await _context.Set
                .Include(s => s.Settype)
                .Include(s => s.ExerciseType)
                .Include(s => s.Session)
                .FirstOrDefaultAsync(m => m.SetId == id);

            if (set == null)
            {
                return NotFound();
            }

            Set = set;
            await PopulateDropDownListsAsync(_context, _userService, Set.ExerciseTypeId, Set.SettypeId, Set.SessionId);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropDownListsAsync(_context, _userService, Set.ExerciseTypeId, Set.SettypeId, Set.SessionId);
                return Page();
            }

            var setToUpdate = await _context.Set
                .Include(s => s.Reps)
                .FirstOrDefaultAsync(m => m.SetId == id);

            if (setToUpdate == null)
            {
                return NotFound();
            }

            // Store original number of reps for comparison
            int originalNumberReps = setToUpdate.NumberReps;
            
            // Store original weight for comparison
            decimal originalWeight = setToUpdate.Weight;

            if (await TryUpdateModelAsync<Set>(
                setToUpdate,
                "Set",
                s => s.Description, s => s.Notes, s => s.SettypeId, s => s.ExerciseTypeId, 
                s => s.SessionId, s => s.NumberReps, s => s.Weight))
            {
                // If NumberReps has changed, update the Rep records
                if (originalNumberReps != setToUpdate.NumberReps)
                {
                    // Get the current reps for this set
                    var currentReps = await _context.Rep
                        .Where(r => r.SetsSetId == setToUpdate.SetId)
                        .OrderBy(r => r.repnumber)
                        .ToListAsync();

                    int currentRepCount = currentReps.Count;

                    // If NumberReps increased, add more Rep records
                    if (setToUpdate.NumberReps > currentRepCount)
                    {
                        for (int i = currentRepCount; i < setToUpdate.NumberReps; i++)
                        {
                            var rep = new Rep
                            {
                                SetsSetId = setToUpdate.SetId,
                                repnumber = i + 1,
                                weight = setToUpdate.Weight, // Use updated weight value
                                success = true
                            };
                            _context.Rep.Add(rep);
                        }
                    }
                    // If NumberReps decreased, remove excess Rep records
                    else if (setToUpdate.NumberReps < currentRepCount)
                    {
                        var repsToRemove = currentReps
                            .OrderByDescending(r => r.repnumber)
                            .Take(currentRepCount - setToUpdate.NumberReps);

                        _context.Rep.RemoveRange(repsToRemove);
                    }
                }
                // If Weight has changed but NumberReps hasn't, update all the Rep weights to match
                else if (originalWeight != setToUpdate.Weight)
                {
                    var currentReps = await _context.Rep
                        .Where(r => r.SetsSetId == setToUpdate.SetId)
                        .ToListAsync();
                        
                    foreach (var rep in currentReps)
                    {
                        rep.weight = setToUpdate.Weight;
                    }
                }

                await _context.SaveChangesAsync();
                return RedirectToPage("./Index");
            }

            await PopulateDropDownListsAsync(_context, _userService, setToUpdate.ExerciseTypeId, setToUpdate.SettypeId, setToUpdate.SessionId);
            return Page();
        }

        private bool SetExists(int id)
        {
            return _context.Set.Any(e => e.SetId == id);
        }
    }
}
