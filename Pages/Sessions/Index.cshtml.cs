using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerweb.Data;
using WorkoutTrackerWeb.Services;
using Microsoft.AspNetCore.Authorization;

namespace WorkoutTrackerWeb.Pages.Sessions
{
    [Authorize] // Require authentication
    public class IndexModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly UserService _userService;

        public IndexModel(
            WorkoutTrackerWebContext context,
            UserService userService)
        {
            _context = context;
            _userService = userService;
        }

        public PaginatedList<Session> Session { get; set; }
        public string DateSort { get; set; }
        public string NameSort { get; set; }
        public string CurrentFilter { get; set; }
        public string CurrentSort { get; set; }

        public async Task OnGetAsync(string sortOrder, string currentFilter, string searchString, int? pageIndex)
        {
            CurrentSort = sortOrder;
            DateSort = String.IsNullOrEmpty(sortOrder) ? "date_asc" : "";
            NameSort = sortOrder == "name" ? "name_desc" : "name";

            if (searchString != null)
            {
                pageIndex = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            CurrentFilter = searchString;

            // Get the current user's ID
            var currentUserId = await _userService.GetCurrentUserIdAsync();
            
            if (currentUserId != null)
            {
                // Get only sessions belonging to the current user
                var sessionsQuery = _context.Session
                    .Include(s => s.User)
                    .Where(s => s.UserId == currentUserId)
                    .AsNoTracking();

                // Apply search filter if any
                if (!String.IsNullOrEmpty(searchString))
                {
                    sessionsQuery = sessionsQuery.Where(s => 
                        s.Name.Contains(searchString));
                }

                // Apply sorting
                switch (sortOrder)
                {
                    case "name":
                        sessionsQuery = sessionsQuery.OrderBy(s => s.Name);
                        break;
                    case "name_desc":
                        sessionsQuery = sessionsQuery.OrderByDescending(s => s.Name);
                        break;
                    case "date_asc":
                        sessionsQuery = sessionsQuery.OrderBy(s => s.datetime);
                        break;
                    default:
                        sessionsQuery = sessionsQuery.OrderByDescending(s => s.datetime); // Default to newest first
                        break;
                }

                int pageSize = 10;
                Session = await PaginatedList<Session>.CreateAsync(
                    sessionsQuery, pageIndex ?? 1, pageSize);
            }
            else
            {
                // Fallback if no current user (shouldn't happen with [Authorize] attribute)
                Session = new PaginatedList<Session>(new List<Session>(), 0, 1, 1);
            }
        }
    }
}
