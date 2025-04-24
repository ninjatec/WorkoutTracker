using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Services;

namespace WorkoutTrackerWeb.Pages.Sets
{
    public class IndexModel : PageModel
    {
        private readonly WorkoutTrackerWeb.Data.WorkoutTrackerWebContext _context;
        private readonly UserService _userService;
        private readonly int _pageSize = 20; // Increase page size for better user experience

        public IndexModel(
            WorkoutTrackerWeb.Data.WorkoutTrackerWebContext context,
            UserService userService)
        {
            _context = context;
            _userService = userService;
        }

        public PaginatedList<Set> Set { get; set; } = default!;
        public string SessionSort { get; set; }
        public string SessionDateTimeSort { get; set; }
        public string ExerciseSort { get; set; }
        public string SetTypeSort { get; set; }
        public string DescriptionSort { get; set; }
        public string NotesSort { get; set; }
        public string NumberRepsSort { get; set; }
        public string WeightSort { get; set; }
        public string CurrentSort { get; set; }
        public string CurrentFilter { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string SearchString { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string FilterType { get; set; }

        public async Task<IActionResult> OnGetAsync(string sortOrder, string currentFilter, string searchString, string filterType, int? pageIndex)
        {
            // Set up sorting parameters
            CurrentSort = sortOrder;
            SessionSort = string.IsNullOrEmpty(sortOrder) ? "session_desc" : "";
            SessionDateTimeSort = sortOrder == "datetime" ? "datetime_desc" : "datetime";
            ExerciseSort = sortOrder == "exercise" ? "exercise_desc" : "exercise";
            SetTypeSort = sortOrder == "settype" ? "settype_desc" : "settype";
            DescriptionSort = sortOrder == "description" ? "description_desc" : "description";
            NotesSort = sortOrder == "notes" ? "notes_desc" : "notes";
            NumberRepsSort = sortOrder == "reps" ? "reps_desc" : "reps";
            WeightSort = sortOrder == "weight" ? "weight_desc" : "weight";
            
            // Reset page index when doing a new search
            if (searchString != null)
            {
                pageIndex = 1;
            }
            else
            {
                searchString = currentFilter;
            }
            
            // Store filter values for view
            CurrentFilter = searchString;
            FilterType = filterType;
            SearchString = searchString;

            // Get current user ID to filter sets by their sessions only
            var currentUserId = await _userService.GetCurrentUserIdAsync();
            if (currentUserId == null)
            {
                // No authenticated user or user not found
                return Challenge();
            }

            // Get the data - use AsNoTracking for read-only operations
            IQueryable<Set> setsIQ = _context.Set
                .AsNoTracking()
                .Include(s => s.Session.User)
                .Include(s => s.ExerciseType)
                .Include(s => s.Settype)
                // Filter by the current user's sessions
                .Where(s => s.Session.UserId == currentUserId);

            // Apply filtering
            if (!string.IsNullOrEmpty(searchString))
            {
                switch (filterType)
                {
                    case "session":
                        setsIQ = setsIQ.Where(s => s.Session.Name.Contains(searchString));
                        break;
                    case "exercise":
                        setsIQ = setsIQ.Where(s => s.ExerciseType.Name.Contains(searchString));
                        break;
                    case "settype":
                        setsIQ = setsIQ.Where(s => s.Settype.Name.Contains(searchString));
                        break;
                    case "description":
                        setsIQ = setsIQ.Where(s => s.Description != null && s.Description.Contains(searchString));
                        break;
                    case "notes":
                        setsIQ = setsIQ.Where(s => s.Notes != null && s.Notes.Contains(searchString));
                        break;
                    default:
                        // Default search across multiple fields - handle nulls safely
                        setsIQ = setsIQ.Where(s => 
                            s.Session.Name.Contains(searchString) ||
                            s.ExerciseType.Name.Contains(searchString) ||
                            s.Settype.Name.Contains(searchString) ||
                            (s.Description != null && s.Description.Contains(searchString)) ||
                            (s.Notes != null && s.Notes.Contains(searchString)));
                        break;
                }
            }

            // Apply sorting based on selected column
            switch (sortOrder)
            {
                case "session_desc":
                    setsIQ = setsIQ.OrderByDescending(s => s.Session.Name);
                    break;
                case "datetime":
                    setsIQ = setsIQ.OrderBy(s => s.Session.datetime);
                    break;
                case "datetime_desc":
                    setsIQ = setsIQ.OrderByDescending(s => s.Session.datetime);
                    break;
                case "exercise":
                    setsIQ = setsIQ.OrderBy(s => s.ExerciseType.Name);
                    break;
                case "exercise_desc":
                    setsIQ = setsIQ.OrderByDescending(s => s.ExerciseType.Name);
                    break;
                case "settype":
                    setsIQ = setsIQ.OrderBy(s => s.Settype.Name);
                    break;
                case "settype_desc":
                    setsIQ = setsIQ.OrderByDescending(s => s.Settype.Name);
                    break;
                case "description":
                    setsIQ = setsIQ.OrderBy(s => s.Description);
                    break;
                case "description_desc":
                    setsIQ = setsIQ.OrderByDescending(s => s.Description);
                    break;
                case "notes":
                    setsIQ = setsIQ.OrderBy(s => s.Notes);
                    break;
                case "notes_desc":
                    setsIQ = setsIQ.OrderByDescending(s => s.Notes);
                    break;
                case "reps":
                    setsIQ = setsIQ.OrderBy(s => s.NumberReps);
                    break;
                case "reps_desc":
                    setsIQ = setsIQ.OrderByDescending(s => s.NumberReps);
                    break;
                case "weight":
                    setsIQ = setsIQ.OrderBy(s => s.Weight);
                    break;
                case "weight_desc":
                    setsIQ = setsIQ.OrderByDescending(s => s.Weight);
                    break;
                default:
                    setsIQ = setsIQ.OrderByDescending(s => s.Session.datetime).ThenBy(s => s.Session.Name);
                    break;
            }

            // Create paginated list instead of loading all records at once
            Set = await PaginatedList<Set>.CreateAsync(
                setsIQ, pageIndex ?? 1, _pageSize);
                
            return Page();
        }
        
        public async Task<IActionResult> OnGetDuplicateAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Find the set to duplicate with related entities
            var setToDuplicate = await _context.Set
                .AsNoTracking() // Use AsNoTracking for initial read
                .Include(s => s.ExerciseType)
                .Include(s => s.Session)
                .Include(s => s.Settype)
                .FirstOrDefaultAsync(m => m.SetId == id);

            if (setToDuplicate == null)
            {
                return NotFound();
            }

            // Get current user ID to verify ownership
            var currentUserId = await _userService.GetCurrentUserIdAsync();
            if (currentUserId == null || setToDuplicate.Session.UserId != currentUserId)
            {
                return Forbid(); // Prevent duplicating sets that don't belong to the current user
            }

            // Find the next available number for the duplicated set
            string baseDescription = setToDuplicate.Description ?? "";
            
            // Check if the description already ends with " (#)" pattern
            var match = Regex.Match(baseDescription, @"(.*) \((\d+)\)$");
            if (match.Success)
            {
                // Extract base name without the number suffix
                baseDescription = match.Groups[1].Value;
            }

            // First, get the descriptions that match our pattern from the database
            var matchingDescriptions = await _context.Set
                .Where(s => s.SessionId == setToDuplicate.SessionId && s.Description != null)
                .Where(s => s.Description.StartsWith(baseDescription) && 
                           (s.Description == baseDescription || s.Description.Contains("(")))
                .Select(s => s.Description)
                .ToListAsync();

            // Then, process them in memory to find the highest number
            int maxNumber = 0;
            string pattern = Regex.Escape(baseDescription) + @" \((\d+)\)$";
            
            foreach (var description in matchingDescriptions)
            {
                var numberMatch = Regex.Match(description, pattern);
                if (numberMatch.Success && int.TryParse(numberMatch.Groups[1].Value, out int num))
                {
                    if (num > maxNumber)
                    {
                        maxNumber = num;
                    }
                }
            }
            
            int nextNumber = maxNumber + 1;

            // Create a new set with copied properties
            var newSet = new Set
            {
                Description = $"{baseDescription} ({nextNumber})",
                Notes = setToDuplicate.Notes,
                SettypeId = setToDuplicate.SettypeId,
                ExerciseTypeId = setToDuplicate.ExerciseTypeId,
                SessionId = setToDuplicate.SessionId,
                NumberReps = setToDuplicate.NumberReps,
                Weight = setToDuplicate.Weight
            };

            // Using a transaction to ensure both operations complete
            using var transaction = await _context.Database.BeginTransactionAsync();
            try 
            {
                // Add the new set to the database
                _context.Set.Add(newSet);
                await _context.SaveChangesAsync();

                // Automatically create Rep records based on NumberReps
                if (newSet.NumberReps > 0)
                {
                    // Create reps in batches if there are many
                    var reps = new List<Rep>();
                    for (int i = 0; i < newSet.NumberReps; i++)
                    {
                        reps.Add(new Rep 
                        { 
                            SetsSetId = newSet.SetId,
                            repnumber = i + 1,
                            weight = newSet.Weight, // Use the set's weight
                            success = true
                        });
                    }
                    
                    _context.Rep.AddRange(reps);
                    await _context.SaveChangesAsync();
                }
                
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
            
            return RedirectToPage("./Index");
        }
    }
}
