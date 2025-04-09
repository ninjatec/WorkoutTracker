using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerweb.Data;

namespace WorkoutTrackerWeb.Pages.Sets
{
    public class CreateModel : SetInputPageModel
    {
        private readonly WorkoutTrackerweb.Data.WorkoutTrackerWebContext _context;

        public CreateModel(WorkoutTrackerweb.Data.WorkoutTrackerWebContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            PopulateSettypeNameDropDownList(_context);
            PopulateExerciseNameDropDownList(_context);
            return Page();
        }

        [BindProperty]
        public Set Set { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                PopulateSettypeNameDropDownList(_context);
                PopulateExerciseNameDropDownList(_context);
                return Page();
            }

            _context.Set.Add(Set);
            await _context.SaveChangesAsync();

            // Automatically create Rep records based on NumberReps
            if (Set.NumberReps > 0)
            {
                for (int i = 0; i < Set.NumberReps; i++)
                {
                    var rep = new Rep 
                    { 
                        SetsSetId = Set.SetId,
                        repnumber = i + 1,
                        weight = 0,
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
