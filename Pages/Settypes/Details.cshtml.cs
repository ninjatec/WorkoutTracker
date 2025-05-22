using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Data;

namespace WorkoutTrackerWeb.Pages.Settypes
{
    public class DetailsModel : PageModel
    {
        private readonly WorkoutTrackerWeb.Data.WorkoutTrackerWebContext _context;

        public DetailsModel(WorkoutTrackerWeb.Data.WorkoutTrackerWebContext context)
        {
            _context = context;
        }

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
    }
}
