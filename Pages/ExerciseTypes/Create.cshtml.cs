using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerweb.Data;

namespace WorkoutTrackerWeb.Pages.ExerciseTypes
{
    public class CreateModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;

        public CreateModel(WorkoutTrackerWebContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public ExerciseType ExerciseType { get; set; } = default!;
        
        // To protect from overposting attacks, see https://aka.ms/RazorPagesCRUD
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid || ExerciseType == null)
            {
                return Page();
            }

            // Set properties for manually created exercises
            ExerciseType.IsFromApi = false;
            ExerciseType.LastUpdated = DateTime.UtcNow;
            
            // If no description was provided, generate one based on available data
            if (string.IsNullOrWhiteSpace(ExerciseType.Description))
            {
                var description = ExerciseType.Name;
                
                if (!string.IsNullOrWhiteSpace(ExerciseType.Type))
                {
                    description += $" - {ExerciseType.Type} exercise";
                }
                
                if (!string.IsNullOrWhiteSpace(ExerciseType.Muscle))
                {
                    description += $" for {ExerciseType.Muscle}";
                }
                
                ExerciseType.Description = description;
            }

            _context.ExerciseType.Add(ExerciseType);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}