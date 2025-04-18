using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerweb.Data;
using WorkoutTrackerWeb.Models;

namespace WorkoutTrackerWeb.Pages.Feedback
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;

        public IndexModel(WorkoutTrackerWebContext context)
        {
            _context = context;
        }

        public PaginatedList<Models.Feedback> Feedback { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string CurrentFilter { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string SearchString { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string TypeFilter { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string CurrentSort { get; set; }
        
        public string SubjectSort { get; set; }
        public string DateSort { get; set; }
        public string StatusSort { get; set; }
        public string TypeSort { get; set; }
        
        public SelectList StatusFilterOptions { get; set; }
        public SelectList TypeFilterOptions { get; set; }

        public async Task OnGetAsync(string sortOrder, string currentFilter, string typeFilter, string searchString, int? pageIndex)
        {
            CurrentSort = sortOrder;
            
            // Define sort orders
            SubjectSort = String.IsNullOrEmpty(sortOrder) ? "subject_desc" : "";
            DateSort = sortOrder == "date" ? "date_desc" : "date";
            StatusSort = sortOrder == "status" ? "status_desc" : "status";
            TypeSort = sortOrder == "type" ? "type_desc" : "type";
            
            // Handle search and filter persistence
            if (searchString != null)
            {
                pageIndex = 1;
            }
            else
            {
                searchString = SearchString;
            }
            
            if (currentFilter != null)
            {
                pageIndex = 1;
            }
            else
            {
                currentFilter = CurrentFilter;
            }
            
            if (typeFilter != null)
            {
                pageIndex = 1;
            }
            else
            {
                typeFilter = TypeFilter;
            }

            SearchString = searchString;
            CurrentFilter = currentFilter;
            TypeFilter = typeFilter;
            
            // Initialize the filter dropdowns
            SetupFilterDropdowns();
            
            // Start with all feedback items accessible to the current user
            IQueryable<Models.Feedback> feedbackQuery = _context.Feedback;
            
            // Apply search filter
            if (!string.IsNullOrEmpty(searchString))
            {
                feedbackQuery = feedbackQuery.Where(f => 
                    f.Subject.Contains(searchString) ||
                    f.Message.Contains(searchString) ||
                    f.ContactEmail.Contains(searchString));
            }
            
            // Apply status filter
            if (!string.IsNullOrEmpty(currentFilter) && Enum.TryParse<FeedbackStatus>(currentFilter, out var statusFilter))
            {
                feedbackQuery = feedbackQuery.Where(f => f.Status == statusFilter);
            }
            
            // Apply type filter
            if (!string.IsNullOrEmpty(typeFilter) && Enum.TryParse<FeedbackType>(typeFilter, out var feedbackTypeFilter))
            {
                feedbackQuery = feedbackQuery.Where(f => f.Type == feedbackTypeFilter);
            }
            
            // Apply sorting
            feedbackQuery = sortOrder switch
            {
                "subject_desc" => feedbackQuery.OrderByDescending(f => f.Subject),
                "date" => feedbackQuery.OrderBy(f => f.SubmissionDate),
                "date_desc" => feedbackQuery.OrderByDescending(f => f.SubmissionDate),
                "status" => feedbackQuery.OrderBy(f => f.Status),
                "status_desc" => feedbackQuery.OrderByDescending(f => f.Status),
                "type" => feedbackQuery.OrderBy(f => f.Type),
                "type_desc" => feedbackQuery.OrderByDescending(f => f.Type),
                _ => feedbackQuery.OrderBy(f => f.Subject)
            };
            
            // Default to 10 items per page
            int pageSize = 10;
            Feedback = await PaginatedList<Models.Feedback>.CreateAsync(
                feedbackQuery.Include(f => f.User).AsNoTracking(),
                pageIndex ?? 1,
                pageSize);
        }
        
        private void SetupFilterDropdowns()
        {
            // Status filter dropdown
            StatusFilterOptions = new SelectList(Enum.GetValues(typeof(FeedbackStatus))
                .Cast<FeedbackStatus>()
                .Select(s => new { Value = s.ToString(), Text = s.ToString() }), 
                "Value", "Text");
                
            // Type filter dropdown
            TypeFilterOptions = new SelectList(Enum.GetValues(typeof(FeedbackType))
                .Cast<FeedbackType>()
                .Select(t => new { Value = t.ToString(), Text = t.ToString() }), 
                "Value", "Text");
        }
    }
    
    public class PaginatedList<T> : List<T>
    {
        public int PageIndex { get; private set; }
        public int TotalPages { get; private set; }
        public int TotalCount { get; private set; }

        public PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
        {
            PageIndex = pageIndex;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);
            TotalCount = count;
            
            this.AddRange(items);
        }

        public bool HasPreviousPage => PageIndex > 1;

        public bool HasNextPage => PageIndex < TotalPages;

        public static async Task<PaginatedList<T>> CreateAsync(
            IQueryable<T> source, int pageIndex, int pageSize)
        {
            var count = await source.CountAsync();
            var items = await source.Skip(
                (pageIndex - 1) * pageSize)
                .Take(pageSize).ToListAsync();
            return new PaginatedList<T>(items, count, pageIndex, pageSize);
        }
    }
}