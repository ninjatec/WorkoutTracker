using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerweb.Data;

namespace WorkoutTrackerWeb.Pages.Sessions
{
    public class DeleteModel : PageModel
    {
        private readonly WorkoutTrackerweb.Data.WorkoutTrackerWebContext _context;

        public DeleteModel(WorkoutTrackerweb.Data.WorkoutTrackerWebContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Session Session { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var session = await _context.Session.FirstOrDefaultAsync(m => m.SessionId == id);

            if (session is not null)
            {
                Session = session;

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

            var session = await _context.Session.FindAsync(id);
            if (session != null)
            {
                Session = session;
                _context.Session.Remove(Session);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
