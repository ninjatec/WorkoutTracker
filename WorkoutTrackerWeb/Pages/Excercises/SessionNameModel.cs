using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WorkoutTrackerWeb.Pages.Excercises
{
    public class SessionNamePageModel : PageModel
    {
        public SelectList SessionNameSL { get; set; }

        public void PopulateSessionNameDropDownList(WorkoutTrackerweb.Data.WorkoutTrackerWebContext _context, 
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