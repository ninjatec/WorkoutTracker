using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WorkoutTrackerWeb.Pages.Sessions
{
    public class UserNamePageModel : PageModel
    {
        public SelectList UserNameSL { get; set; }

        public void PopulateUserNameDropDownList(WorkoutTrackerweb.Data.WorkoutTrackerWebContext _context, 
            object selectedUser = null)
        {
            var userQuery = from u in _context.User
                            orderby u.Name
                            select u;

            UserNameSL = new SelectList(userQuery.AsNoTracking(), 
                nameof(Models.User.UserId), 
                nameof(Models.User.Name),
                selectedUser);
        }
    }
}