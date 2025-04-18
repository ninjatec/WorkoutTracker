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
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WorkoutTrackerWeb.Pages.Sessions
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly UserService _userService;

        public DetailsModel(WorkoutTrackerWebContext context, UserService userService)
        {
            _context = context;
            _userService = userService;
        }

        public Session Session { get; set; } = default!;
        
        [BindProperty(SupportsGet = true)]
        public string SortField { get; set; } = "Default";
        
        [BindProperty(SupportsGet = true)]
        public string SortOrder { get; set; } = "asc";
        
        public string CurrentSort { get; set; }
        
        public List<SelectListItem> SortOptions { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "Default", Text = "Default" },
            new SelectListItem { Value = "ExerciseType", Text = "Exercise Type Only" },
            new SelectListItem { Value = "SetType", Text = "Set Type Only" },
            new SelectListItem { Value = "NumberReps", Text = "Number of Reps" },
            new SelectListItem { Value = "Weight", Text = "Weight (kg)" },
            new SelectListItem { Value = "SetID", Text = "Set ID" }
        };

        public async Task<IActionResult> OnGetAsync(int? id, string sortField, string sortOrder)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Apply sorting parameters if provided
            if (!string.IsNullOrEmpty(sortField))
            {
                SortField = sortField;
            }
            
            if (!string.IsNullOrEmpty(sortOrder))
            {
                SortOrder = sortOrder;
            }
            
            CurrentSort = SortField;

            // Get the current user id
            var currentUserId = await _userService.GetCurrentUserIdAsync();
            if (currentUserId == null)
            {
                return Challenge(); // Redirect to login if not authenticated
            }

            // Get the session with ownership check and include Sets and Reps
            var session = await _context.Session
                .Include(s => s.User)
                .Include(s => s.Sets)
                    .ThenInclude(set => set.ExerciseType)
                .Include(s => s.Sets)
                    .ThenInclude(set => set.Settype)
                .Include(s => s.Sets)
                    .ThenInclude(set => set.Reps.OrderBy(r => r.repnumber))
                .FirstOrDefaultAsync(m => m.SessionId == id && m.UserId == currentUserId);

            if (session == null)
            {
                return NotFound();
            }

            // Apply sorting to the sets
            if (session.Sets != null && session.Sets.Any())
            {
                session.Sets = SortOrder.ToLower() == "asc" 
                    ? SortSetsAscending(session.Sets, SortField).ToList()
                    : SortSetsDescending(session.Sets, SortField).ToList();
            }

            Session = session;
            return Page();
        }

        private IEnumerable<Set> SortSetsAscending(IEnumerable<Set> sets, string sortField)
        {
            return sortField switch
            {
                "Default" => sets.OrderBy(s => s.ExerciseType.Name)
                               .ThenBy(s => s.Settype.Name)
                               .ThenBy(s => s.SetId),
                "SetType" => sets.OrderBy(s => s.Settype.Name),
                "NumberReps" => sets.OrderBy(s => s.NumberReps),
                "Weight" => sets.OrderBy(s => s.Weight),
                "SetID" => sets.OrderBy(s => s.SetId),
                _ => sets.OrderBy(s => s.ExerciseType.Name) // Default to ExerciseType only
            };
        }

        private IEnumerable<Set> SortSetsDescending(IEnumerable<Set> sets, string sortField)
        {
            return sortField switch
            {
                "Default" => sets.OrderByDescending(s => s.ExerciseType.Name)
                               .ThenByDescending(s => s.Settype.Name)
                               .ThenByDescending(s => s.SetId),
                "SetType" => sets.OrderByDescending(s => s.Settype.Name),
                "NumberReps" => sets.OrderByDescending(s => s.NumberReps),
                "Weight" => sets.OrderByDescending(s => s.Weight),
                "SetID" => sets.OrderByDescending(s => s.SetId),
                _ => sets.OrderByDescending(s => s.ExerciseType.Name) // Default to ExerciseType only
            };
        }
    }
}
