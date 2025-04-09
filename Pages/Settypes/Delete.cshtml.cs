using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerweb.Data;

namespace WorkoutTrackerWeb.Pages.Settypes
{
    public class DeleteModel : PageModel
    {
        private readonly WorkoutTrackerweb.Data.WorkoutTrackerWebContext _context;

        public DeleteModel(WorkoutTrackerweb.Data.WorkoutTrackerWebContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Settype Settype { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var settype = await _context.Settype.FirstOrDefaultAsync(m => m.SettypeId == id);

            if (settype is not null)
            {
                Settype = settype;

                return Page();
            }

            return NotFound();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var settype = await _context.Settype.FindAsync(id);
            if (settype != null)
            {
                Settype = settype;
                _context.Settype.Remove(Settype);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
