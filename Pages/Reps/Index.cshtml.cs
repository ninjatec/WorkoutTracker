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
        public string SetTypeSort { get; set; }
        public string WeightSort { get; set; }
        public string RepNumberSort { get; set; }
        public string SuccessSort { get; set; }
        public string CurrentSort { get; set; }
        public string CurrentFilter { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string SearchString { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string FilterType { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public bool? SuccessFilter { get; set; }

        public async Task OnGetAsync(string sortOrder, string currentFilter, string searchString, string filterType, bool? successFilter)
        {
            // Set up sorting parameters
            CurrentSort = sortOrder;
            SessionSort = string.IsNullOrEmpty(sortOrder) ? "session_desc" : "";
            SessionDateTimeSort = sortOrder == "datetime" ? "datetime_desc" : "datetime";
            ExerciseSort = sortOrder == "exercise" ? "exercise_desc" : "exercise";
            SetTypeSort = sortOrder == "settype" ? "settype_desc" : "settype";
            WeightSort = sortOrder == "weight" ? "weight_desc" : "weight";
            RepNumberSort = sortOrder == "repnumber" ? "repnumber_desc" : "repnumber";
            SuccessSort = sortOrder == "success" ? "success_desc" : "success";
            
            // Handle search/filter parameters
            if (searchString != null)
            {
                // New search, reset page
                CurrentFilter = searchString;
            }
            else
            {
                searchString = currentFilter;
            }
            
            // Store filter values for view
            CurrentFilter = searchString;
            FilterType = filterType;
            SearchString = searchString;
            SuccessFilter = successFilter;
            
            IQueryable<Rep> repsIQ = _context.Rep
                .Include(r => r.Sets)
                    .ThenInclude(s => s.Session)
                .Include(r => r.Sets)
                    .ThenInclude(s => s.ExerciseType)
                .Include(r => r.Sets)
                    .ThenInclude(s => s.Settype);
            
            // Apply filtering
            if (!string.IsNullOrEmpty(searchString))
            {
                switch (filterType)
                {
                    case "session":
                        repsIQ = repsIQ.Where(r => r.Sets.Session.Name.Contains(searchString));
                        break;
                    case "exercise":
                        repsIQ = repsIQ.Where(r => r.Sets.ExerciseType.Name.Contains(searchString));
                        break;
                    case "settype":
                        repsIQ = repsIQ.Where(r => r.Sets.Settype.Name.Contains(searchString));
                        break;
                    case "weight":
                        // Try to parse the search string as a decimal for weight comparison
                        if (decimal.TryParse(searchString, out decimal weightValue))
                        {
                            repsIQ = repsIQ.Where(r => r.weight == weightValue);
                        }
                        break;
                    case "repnumber":
                        // Try to parse the search string as an int for rep number comparison
                        if (int.TryParse(searchString, out int repNumber))
                        {
                            repsIQ = repsIQ.Where(r => r.repnumber == repNumber);
                        }
                        break;
                    default:
                        // Default search across multiple fields
                        repsIQ = repsIQ.Where(r => 
                            r.Sets.Session.Name.Contains(searchString) ||
                            r.Sets.ExerciseType.Name.Contains(searchString) ||
                            r.Sets.Settype.Name.Contains(searchString));
                        break;
                }
            }
            
            // Apply success/fail filter if selected
            if (successFilter.HasValue)
            {
                repsIQ = repsIQ.Where(r => r.success == successFilter.Value);
            }
                    
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
                case "settype":
                    repsIQ = repsIQ.OrderBy(r => r.Sets.Settype.Name);
                    break;
                case "settype_desc":
                    repsIQ = repsIQ.OrderByDescending(r => r.Sets.Settype.Name);
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
                .Include(s => s.Settype)
                .OrderBy(s => s.SetId)
                .ToListAsync();
        }
    }
}
