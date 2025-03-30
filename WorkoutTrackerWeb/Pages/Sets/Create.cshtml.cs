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
    public class CreateModel : PageModel
    {
        private readonly WorkoutTrackerweb.Data.WorkoutTrackerWebContext _context;

        public CreateModel(WorkoutTrackerweb.Data.WorkoutTrackerWebContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
        ViewData["ExcerciseId"] = new SelectList(_context.Excercise, "ExcerciseId", "ExcerciseId");
        ViewData["SessionId"] = new SelectList(_context.Session, "SessionId", "SessionId");
        ViewData["UserId"] = new SelectList(_context.Set<User>(), "UserId", "UserId");
            return Page();
        }

        [BindProperty]
        public aSet aSet { get; set; } = default!;

        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.aSet.Add(aSet);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
