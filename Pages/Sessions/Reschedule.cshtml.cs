using System;
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
        public WorkoutSession WorkoutSession { get; set; } = default!;
        
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

            var currentUserId = await _userService.GetCurrentUserIdAsync();
            if (currentUserId == null)
            {
                return Challenge();
            }

            WorkoutSession = await _context.WorkoutSessions
                .Include(ws => ws.User)
                .FirstOrDefaultAsync(ws => ws.WorkoutSessionId == id && ws.UserId == currentUserId);

            if (WorkoutSession == null)
            {
                return NotFound();
            }
            
            // Make sure this is actually a missed workout
            if (WorkoutSession.Status != "Missed")
            {
                TempData["ErrorMessage"] = "This workout is not marked as missed and cannot be rescheduled.";
                return RedirectToPage("./Index");
            }
            
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var currentUserId = await _userService.GetCurrentUserIdAsync();
            if (currentUserId == null)
            {
                return Challenge();
            }

            var workoutSession = await _context.WorkoutSessions
                .FirstOrDefaultAsync(ws => ws.WorkoutSessionId == WorkoutSession.WorkoutSessionId && 
                                         ws.UserId == currentUserId);

            if (workoutSession == null)
            {
                return NotFound();
            }
                    
            if (workoutSession.Status != "Missed")
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
                // Update workoutSession
                workoutSession.StartDateTime = NewDateTime;
                workoutSession.Status = "Scheduled"; // Change from "Missed" to "Scheduled"
                
                // Add a note about rescheduling
                var oldDateTime = workoutSession.StartDateTime;
                if (string.IsNullOrEmpty(workoutSession.Description))
                {
                    workoutSession.Description = $"Rescheduled from {oldDateTime:g}";
                }
                else
                {
                    workoutSession.Description += $"\nRescheduled from {oldDateTime:g}";
                }
                
                // Copy over any additional changes from the form
                if (!string.IsNullOrEmpty(WorkoutSession.Description))
                {
                    workoutSession.Description = WorkoutSession.Description;
                }
                
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Workout successfully rescheduled.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!WorkoutSessionExists(WorkoutSession.WorkoutSessionId))
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

        private bool WorkoutSessionExists(int id)
        {
            return _context.WorkoutSessions.Any(ws => ws.WorkoutSessionId == id);
        }
    }
}