using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerweb.Data;

namespace WorkoutTrackerWeb.Pages.Excercises
{
    public class CreateModel : SessionNamePageModel
    {
        private readonly WorkoutTrackerweb.Data.WorkoutTrackerWebContext _context;

        public CreateModel(WorkoutTrackerweb.Data.WorkoutTrackerWebContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            PopulateSessionNameDropDownList(_context);
            return Page();
        }

        [BindProperty]
        public Excercise Excercise { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                PopulateSessionNameDropDownList(_context);
                return Page();
            }

            _context.Excercise.Add(Excercise);
            await _context.SaveChangesAsync();
            return RedirectToPage("./Index");
        }
    }
}
