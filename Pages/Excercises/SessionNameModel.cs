using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using WorkoutTrackerWeb.Services;
using System.Threading.Tasks;

namespace WorkoutTrackerWeb.Pages.Excercises
{
    public class SessionNamePageModel : PageModel
    {
        public SelectList SessionNameSL { get; set; }

        // Method for use when UserService is available
        public async Task PopulateSessionNameDropDownListAsync(
            WorkoutTrackerweb.Data.WorkoutTrackerWebContext _context,
            UserService userService,
            object selectedSession = null)
        {
            // Get current user ID
            var currentUserId = await userService.GetCurrentUserIdAsync();

            // Filter sessions by current user
            var sessionQuery = from s in _context.Session
                              where s.UserId == currentUserId
                              orderby s.Name
                              select s;

            SessionNameSL = new SelectList(await sessionQuery.AsNoTracking().ToListAsync(), 
                nameof(Models.Session.SessionId), 
                nameof(Models.Session.Name),
                selectedSession);
        }

        // Legacy method for backward compatibility
        public void PopulateSessionNameDropDownList(
            WorkoutTrackerweb.Data.WorkoutTrackerWebContext _context, 
            object selectedSession = null)
        {
            var sessionQuery = from s in _context.Session
                            orderby s.Name
                            select s;

            SessionNameSL = new SelectList(sessionQuery.AsNoTracking(), 
                nameof(Models.Session.SessionId), 
                nameof(Models.Session.Name),
                selectedSession);
        }
    }
}