using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Services.Email;

namespace WorkoutTrackerWeb.Areas.Admin.Pages.Feedback
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly IEmailService _emailService;

        public IndexModel(WorkoutTrackerWebContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
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
        
        [BindProperty(SupportsGet = true)]
        public bool OnlyRecentFeedback { get; set; }
        
        public string SubjectSort { get; set; }
        public string DateSort { get; set; }
        public string StatusSort { get; set; }
        public string TypeSort { get; set; }
        public string PrioritySort { get; set; }
        
        public SelectList StatusFilterOptions { get; set; }
        public SelectList TypeFilterOptions { get; set; }
        public SelectList PriorityFilterOptions { get; set; }
        
        public int TotalFeedbackCount { get; set; }
        public int NewFeedbackCount { get; set; }
        public int InProgressFeedbackCount { get; set; }
        public int CompletedFeedbackCount { get; set; }
        public int RejectedFeedbackCount { get; set; }
        
        public List<string> FeedbackTypeLabels { get; set; } = new List<string>();
        public List<int> FeedbackTypeData { get; set; } = new List<int>();
        
        public List<string> FeedbackStatusLabels { get; set; } = new List<string>();
        public List<int> FeedbackStatusData { get; set; } = new List<int>();
        
        [TempData]
        public string StatusMessage { get; set; }
        
        [TempData]
        public bool IsSuccess { get; set; }
        
        public class FeedbackActivityViewModel
        {
            public int FeedbackId { get; set; }
            public string Subject { get; set; }
            public string Description { get; set; }
            public DateTime Date { get; set; }
            public FeedbackStatus Status { get; set; }
        }
        
        public List<FeedbackActivityViewModel> RecentActivity { get; set; } = new List<FeedbackActivityViewModel>();

        public async Task OnGetAsync(string sortOrder, string currentFilter, string typeFilter, string searchString, bool? onlyRecentFeedback, int? pageIndex)
        {
            CurrentSort = sortOrder;
            
            // Define sort orders
            SubjectSort = String.IsNullOrEmpty(sortOrder) ? "subject_desc" : "";
            DateSort = sortOrder == "date" ? "date_desc" : "date";
            StatusSort = sortOrder == "status" ? "status_desc" : "status";
            TypeSort = sortOrder == "type" ? "type_desc" : "type";
            PrioritySort = sortOrder == "priority" ? "priority_desc" : "priority";
            
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
            
            if (onlyRecentFeedback.HasValue)
            {
                OnlyRecentFeedback = onlyRecentFeedback.Value;
                pageIndex = 1;
            }

            SearchString = searchString;
            CurrentFilter = currentFilter;
            TypeFilter = typeFilter;
            
            // Initialize the filter dropdowns
            SetupFilterDropdowns();
            
            // Collect statistics for dashboard
            await CollectFeedbackStatistics();
            
            // Create recent activity list
            await GenerateRecentActivityList();
            
            // Start with all feedback items
            IQueryable<Models.Feedback> feedbackQuery = _context.Feedback.Include(f => f.User);
            
            // Apply search filter
            if (!string.IsNullOrEmpty(searchString))
            {
                feedbackQuery = feedbackQuery.Where(f => 
                    f.Subject.Contains(searchString) ||
                    f.Message.Contains(searchString) ||
                    (f.ContactEmail != null && f.ContactEmail.Contains(searchString)) ||
                    (f.AdminNotes != null && f.AdminNotes.Contains(searchString)));
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
            
            // Apply date filter (last 30 days)
            if (OnlyRecentFeedback)
            {
                var thirtyDaysAgo = DateTime.Now.AddDays(-30);
                feedbackQuery = feedbackQuery.Where(f => f.SubmissionDate >= thirtyDaysAgo);
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
                "priority" => feedbackQuery.OrderBy(f => f.Priority),
                "priority_desc" => feedbackQuery.OrderByDescending(f => f.Priority),
                _ => feedbackQuery.OrderBy(f => f.Subject)
            };
            
            // Default to 20 items per page for admin (more than regular user view)
            int pageSize = 20;
            Feedback = await PaginatedList<Models.Feedback>.CreateAsync(
                feedbackQuery.AsNoTracking(),
                pageIndex ?? 1,
                pageSize);
        }
        
        public async Task<IActionResult> OnPostQuickUpdateAsync(int id, int status, string note, bool notifyUser)
        {
            return await QuickUpdateAsync(id, status, note, notifyUser);
        }
        
        public async Task<IActionResult> QuickUpdateAsync(int id, int status, string note, bool notifyUser)
        {
            if (id <= 0 || !Enum.IsDefined(typeof(FeedbackStatus), status))
            {
                IsSuccess = false;
                StatusMessage = "Invalid request parameters.";
                return RedirectToPage();
            }
            
            var feedback = await _context.Feedback.FindAsync(id);
            
            if (feedback == null)
            {
                IsSuccess = false;
                StatusMessage = "Feedback item not found.";
                return RedirectToPage();
            }
            
            // Store the old status for change tracking
            var oldStatus = feedback.Status;
            
            // Update status
            feedback.Status = (FeedbackStatus)status;
            feedback.LastUpdated = DateTime.Now;
            
            // Add status change note if provided
            if (!string.IsNullOrEmpty(note))
            {
                string timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                string statusNote = $"[{timestamp}] {note}";
                
                if (string.IsNullOrEmpty(feedback.AdminNotes))
                {
                    feedback.AdminNotes = statusNote;
                }
                else
                {
                    feedback.AdminNotes = statusNote + "\n\n" + feedback.AdminNotes;
                }
            }
            
            try
            {
                // Explicitly mark the entity as modified to ensure changes are tracked
                _context.Entry(feedback).State = EntityState.Modified;
                
                await _context.SaveChangesAsync();
                
                // Refresh statistics immediately after database update
                await CollectFeedbackStatistics();
                
                // Send notification email if requested
                if (notifyUser && !string.IsNullOrEmpty(feedback.ContactEmail))
                {
                    await SendStatusUpdateEmail(feedback, oldStatus);
                }
                
                IsSuccess = true;
                StatusMessage = $"Feedback #{id} has been updated to {((FeedbackStatus)status).ToString()}.";
            }
            catch (Exception ex)
            {
                IsSuccess = false;
                StatusMessage = $"Error updating feedback: {ex.Message}";
            }
            
            // Force current page reload with refreshed data
            return RedirectToPage(new { currentFilter = CurrentFilter, searchString = SearchString, typeFilter = TypeFilter });
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
                
            // Priority filter dropdown
            PriorityFilterOptions = new SelectList(Enum.GetValues(typeof(FeedbackPriority))
                .Cast<FeedbackPriority>()
                .Select(p => new { Value = p.ToString(), Text = p.ToString() }), 
                "Value", "Text");
        }
        
        private async Task CollectFeedbackStatistics()
        {
            // Count feedback by status for dashboard cards
            TotalFeedbackCount = await _context.Feedback.CountAsync();
            NewFeedbackCount = await _context.Feedback.CountAsync(f => f.Status == FeedbackStatus.New);
            InProgressFeedbackCount = await _context.Feedback.CountAsync(f => f.Status == FeedbackStatus.InProgress);
            CompletedFeedbackCount = await _context.Feedback.CountAsync(f => f.Status == FeedbackStatus.Completed);
            RejectedFeedbackCount = await _context.Feedback.CountAsync(f => f.Status == FeedbackStatus.Rejected);
            
            // Data for feedback type chart
            var feedbackTypeStats = await _context.Feedback
                .GroupBy(f => f.Type)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToListAsync();
                
            FeedbackTypeLabels = new List<string>();
            FeedbackTypeData = new List<int>();
            
            foreach (var type in Enum.GetValues(typeof(FeedbackType)).Cast<FeedbackType>())
            {
                FeedbackTypeLabels.Add(type.ToString());
                FeedbackTypeData.Add(feedbackTypeStats.FirstOrDefault(s => s.Type == type)?.Count ?? 0);
            }
            
            // Data for feedback status chart
            var feedbackStatusStats = await _context.Feedback
                .GroupBy(f => f.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();
                
            FeedbackStatusLabels = new List<string>();
            FeedbackStatusData = new List<int>();
            
            foreach (var status in Enum.GetValues(typeof(FeedbackStatus)).Cast<FeedbackStatus>())
            {
                FeedbackStatusLabels.Add(status.ToString());
                FeedbackStatusData.Add(feedbackStatusStats.FirstOrDefault(s => s.Status == status)?.Count ?? 0);
            }
        }
        
        private async Task GenerateRecentActivityList()
        {
            // Get the latest 5 feedback items for the activity feed
            var recentFeedback = await _context.Feedback
                .OrderByDescending(f => f.SubmissionDate)
                .Take(5)
                .Select(f => new FeedbackActivityViewModel 
                { 
                    FeedbackId = f.FeedbackId,
                    Subject = f.Subject,
                    Description = "New feedback submitted",
                    Date = f.SubmissionDate,
                    Status = f.Status
                })
                .ToListAsync();
                
            RecentActivity = recentFeedback;
        }
        
        private async Task SendStatusUpdateEmail(Models.Feedback feedback, FeedbackStatus oldStatus)
        {
            var subject = $"Update on your feedback: {feedback.Subject}";
            var message = $"Your feedback has been updated.\n\n" +
                         $"Status changed from: {oldStatus} to: {feedback.Status}\n\n" +
                         $"Subject: {feedback.Subject}\n\n";
                         
            if (!string.IsNullOrEmpty(feedback.AdminNotes))
            {
                // Extract just the most recent note (first section before blank line)
                string recentNote = feedback.AdminNotes;
                int blankLineIndex = feedback.AdminNotes.IndexOf("\n\n");
                if (blankLineIndex > 0)
                {
                    recentNote = feedback.AdminNotes.Substring(0, blankLineIndex);
                }
                
                message += $"Additional notes:\n{recentNote}\n\n";
            }
            
            message += "Thank you for helping us improve Workout Tracker!";
            
            await _emailService.SendFeedbackEmailAsync(subject, message, feedback.ContactEmail);
        }
    }
}