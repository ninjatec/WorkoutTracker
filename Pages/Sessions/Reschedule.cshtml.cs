using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Services;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;

namespace WorkoutTrackerWeb.Pages.Sessions
{
    [Authorize]
    public class RescheduleModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly UserService _userService;

        public RescheduleModel(WorkoutTrackerWebContext context, UserService userService)
        {
            _context = context;
            _userService = userService;
        }

        [BindProperty]
        public Session Session { get; set; } = default!;
        
        public WorkoutSession WorkoutSession { get; set; }
        
        [BindProperty]
        [Required(ErrorMessage = "Please select a new date and time")]
        [Display(Name = "New Workout Date")]
        [DataType(DataType.DateTime)]
        public DateTime NewDateTime { get; set; } = DateTime.Now.AddDays(1);

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Get the current user
            var currentUserId = await _userService.GetCurrentUserIdAsync();
            if (currentUserId == null)
            {
                return Challenge();
            }

            // Get the session with ownership check
            var session = await _context.Session
                .Include(s => s.User)
                .FirstOrDefaultAsync(m => m.SessionId == id && m.UserId == currentUserId);

            if (session == null)
            {
                return NotFound();
            }
            
            // Get the related WorkoutSession 
            WorkoutSession = await _context.WorkoutSessions
                .FirstOrDefaultAsync(ws => 
                    ws.UserId == currentUserId && 
                    ws.StartDateTime == session.StartDateTime);
            
            // Make sure this is actually a missed workout
            if (WorkoutSession == null || WorkoutSession.Status != "Missed")
            {
                TempData["ErrorMessage"] = "This workout is not marked as missed and cannot be rescheduled.";
                return RedirectToPage("./Index");
            }
            
            Session = session;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Get the current user
            var currentUserId = await _userService.GetCurrentUserIdAsync();
            if (currentUserId == null)
            {
                return Challenge();
            }

            // Verify ownership and get the session
            var sessionToUpdate = await _context.Session
                .FirstOrDefaultAsync(s => s.SessionId == Session.SessionId && s.UserId == currentUserId);

            if (sessionToUpdate == null)
            {
                return NotFound();
            }

            // Get the related WorkoutSession
            var workoutSession = await _context.WorkoutSessions
                .FirstOrDefaultAsync(ws => 
                    ws.UserId == currentUserId && 
                    ws.StartDateTime == sessionToUpdate.StartDateTime);
                    
            if (workoutSession == null || workoutSession.Status != "Missed")
            {
                TempData["ErrorMessage"] = "This workout is not marked as missed and cannot be rescheduled.";
                return RedirectToPage("./Index");
            }

            if (!ModelState.IsValid)
            {
                WorkoutSession = workoutSession;
                return Page();
            }

            try
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    // Update session
                    sessionToUpdate.datetime = NewDateTime;
                    sessionToUpdate.StartDateTime = NewDateTime;
                    sessionToUpdate.Notes = Session.Notes;
                    
                    // Update workoutSession
                    workoutSession.StartDateTime = NewDateTime;
                    workoutSession.Status = "Scheduled"; // Change from "Missed" to "Scheduled"
                    
                    // Add a note about rescheduling
                    if (string.IsNullOrEmpty(workoutSession.Description))
                    {
                        workoutSession.Description = $"Rescheduled from {workoutSession.StartDateTime}";
                    }
                    else
                    {
                        workoutSession.Description += $"\nRescheduled from {workoutSession.StartDateTime}";
                    }
                    
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    
                    TempData["SuccessMessage"] = "Workout successfully rescheduled.";
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SessionExists(Session.SessionId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }
        
        public IActionResult OnPostCancel()
        {
            return RedirectToPage("./Index");
        }

        private bool SessionExists(int id)
        {
            return _context.Session.Any(e => e.SessionId == id);
        }
    }
}