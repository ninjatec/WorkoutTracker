using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerweb.Data;

namespace WorkoutTrackerWeb.Pages.Feedback
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;

        public DetailsModel(WorkoutTrackerWebContext context)
        {
            _context = context;
        }

        public Models.Feedback Feedback { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Feedback = await _context.Feedback
                .Include(f => f.User)
                .FirstOrDefaultAsync(m => m.FeedbackId == id);

            if (Feedback == null)
            {
                return NotFound();
            }
            
            return Page();
        }
    }
}