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

namespace WorkoutTrackerWeb.Pages.Reps
{
    public class CreateModel : PageModel
    {
        private readonly WorkoutTrackerweb.Data.WorkoutTrackerWebContext _context;

        public CreateModel(WorkoutTrackerweb.Data.WorkoutTrackerWebContext context)
        {
            _context = context;
        }

        [BindProperty]
        public List<Rep> Reps { get; set; } = new List<Rep>();
        
        public Set Set { get; set; }

        public async Task<IActionResult> OnGetAsync(int? setId)
        {
            if (setId == null)
            {
                return NotFound();
            }

            Set = await _context.Set
                .Include(s => s.ExerciseType)
                .Include(s => s.Session)
                .Include(s => s.Settype)
                .FirstOrDefaultAsync(m => m.SetId == setId);

            if (Set == null)
            {
                return NotFound();
            }

            // Initialize the reps based on NumberReps with the correct weight
            for (int i = 0; i < Set.NumberReps; i++)
            {
                Reps.Add(new Rep { 
                    repnumber = i + 1,
                    weight = Set.Weight, // Use the weight from the Set
                    success = true
                });
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int setId)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            foreach (var rep in Reps)
            {
                rep.Sets = await _context.Set.FindAsync(setId);
                _context.Rep.Add(rep);
            }

            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
