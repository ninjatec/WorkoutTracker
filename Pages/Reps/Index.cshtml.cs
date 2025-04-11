using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerweb.Data;

namespace WorkoutTrackerWeb.Pages.Reps
{
    public class IndexModel : PageModel
    {
        private readonly WorkoutTrackerweb.Data.WorkoutTrackerWebContext _context;

        public IndexModel(WorkoutTrackerweb.Data.WorkoutTrackerWebContext context)
        {
            _context = context;
        }

        public IList<Rep> Rep { get;set; } = default!;
        public IList<Set> AvailableSets { get; set; } = default!;
        
        public string SessionSort { get; set; }
        public string SessionDateTimeSort { get; set; }
        public string ExerciseSort { get; set; }
        public string WeightSort { get; set; }
        public string RepNumberSort { get; set; }
        public string SuccessSort { get; set; }
        public string CurrentSort { get; set; }

        public async Task OnGetAsync(string sortOrder)
        {
            // Set up sorting parameters
            CurrentSort = sortOrder;
            SessionSort = string.IsNullOrEmpty(sortOrder) ? "session_desc" : "";
            SessionDateTimeSort = sortOrder == "datetime" ? "datetime_desc" : "datetime";
            ExerciseSort = sortOrder == "exercise" ? "exercise_desc" : "exercise";
            WeightSort = sortOrder == "weight" ? "weight_desc" : "weight";
            RepNumberSort = sortOrder == "repnumber" ? "repnumber_desc" : "repnumber";
            SuccessSort = sortOrder == "success" ? "success_desc" : "success";
            
            IQueryable<Rep> repsIQ = _context.Rep
                .Include(r => r.Sets)
                    .ThenInclude(s => s.Session)
                .Include(r => r.Sets)
                    .ThenInclude(s => s.ExerciseType);
                    
            // Apply sorting based on selected column
            switch (sortOrder)
            {
                case "session_desc":
                    repsIQ = repsIQ.OrderByDescending(r => r.Sets.Session.Name);
                    break;
                case "datetime":
                    repsIQ = repsIQ.OrderBy(r => r.Sets.Session.datetime);
                    break;
                case "datetime_desc":
                    repsIQ = repsIQ.OrderByDescending(r => r.Sets.Session.datetime);
                    break;
                case "exercise":
                    repsIQ = repsIQ.OrderBy(r => r.Sets.ExerciseType.Name);
                    break;
                case "exercise_desc":
                    repsIQ = repsIQ.OrderByDescending(r => r.Sets.ExerciseType.Name);
                    break;
                case "weight":
                    repsIQ = repsIQ.OrderBy(r => r.weight);
                    break;
                case "weight_desc":
                    repsIQ = repsIQ.OrderByDescending(r => r.weight);
                    break;
                case "repnumber":
                    repsIQ = repsIQ.OrderBy(r => r.repnumber);
                    break;
                case "repnumber_desc":
                    repsIQ = repsIQ.OrderByDescending(r => r.repnumber);
                    break;
                case "success":
                    repsIQ = repsIQ.OrderBy(r => r.success);
                    break;
                case "success_desc":
                    repsIQ = repsIQ.OrderByDescending(r => r.success);
                    break;
                default:
                    repsIQ = repsIQ.OrderBy(r => r.Sets.Session.Name);
                    break;
            }

            Rep = await repsIQ.ToListAsync();

            AvailableSets = await _context.Set
                .Include(s => s.ExerciseType)
                .Include(s => s.Session)
                .OrderBy(s => s.SetId)
                .ToListAsync();
        }
    }
}
