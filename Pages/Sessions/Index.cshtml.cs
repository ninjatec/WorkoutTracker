using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Services;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;

namespace WorkoutTrackerWeb.Pages.Sessions
{
    [Authorize]
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

        public PaginatedList<WorkoutSession> WorkoutSessions { get; set; }
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

            var currentUserId = await _userService.GetCurrentUserIdAsync();
            
            if (currentUserId != null)
            {
                var query = _context.WorkoutSessions
                    .Include(ws => ws.User)
                    .Where(ws => ws.UserId == currentUserId);

                // Apply search filter if any
                if (!String.IsNullOrEmpty(searchString))
                {
                    query = query.Where(ws => ws.Name.Contains(searchString));
                }

                // Apply sorting
                query = sortOrder switch
                {
                    "name" => query.OrderBy(ws => ws.Name),
                    "name_desc" => query.OrderByDescending(ws => ws.Name),
                    "date_asc" => query.OrderBy(ws => ws.StartDateTime),
                    _ => query.OrderByDescending(ws => ws.StartDateTime)
                };

                int pageSize = 10;
                WorkoutSessions = await PaginatedList<WorkoutSession>.CreateAsync(
                    query, pageIndex ?? 1, pageSize);
            }
            else
            {
                WorkoutSessions = new PaginatedList<WorkoutSession>(new List<WorkoutSession>(), 0, 1, 1);
            }
        }
    }
}
