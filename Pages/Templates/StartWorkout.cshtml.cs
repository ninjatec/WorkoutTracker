using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;

namespace WorkoutTrackerWeb.Pages.Templates
{
    [Authorize]
    public class StartWorkoutModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;

        public StartWorkoutModel(WorkoutTrackerWebContext context)
        {
            _context = context;
        }

        public WorkoutTemplate WorkoutTemplate { get; set; } = default!;
        public string DefaultSessionName { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var workoutTemplate = await _context.WorkoutTemplate
                .Include(t => t.TemplateExercises)
                .ThenInclude(e => e.ExerciseType)
                .Include(t => t.TemplateExercises)
                .ThenInclude(e => e.TemplateSets)
                .ThenInclude(s => s.Settype)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.WorkoutTemplateId == id);

            if (workoutTemplate == null)
            {
                return NotFound();
            }

            // Generate a default session name based on template name and date
            DefaultSessionName = $"{workoutTemplate.Name} - {DateTime.Now:yyyy-MM-dd}";

            WorkoutTemplate = workoutTemplate;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int templateId, string sessionName, DateTime sessionDate, string sessionNotes)
        {
            // Validate template exists
            var template = await _context.WorkoutTemplate
                .Include(t => t.TemplateExercises)
                .ThenInclude(e => e.ExerciseType)
                .Include(t => t.TemplateExercises)
                .ThenInclude(e => e.TemplateSets)
                .ThenInclude(s => s.Settype)
                .FirstOrDefaultAsync(t => t.WorkoutTemplateId == templateId);

            if (template == null)
            {
                return NotFound();
            }

            // Get current user
            var currentUser = await _context.GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToPage("/Account/Login");
            }

            // Create a new session
            var session = new Session
            {
                Name = sessionName,
                datetime = sessionDate,
                UserId = currentUser.UserId,
                Notes = sessionNotes ?? string.Empty,
                Sets = new List<Set>()
            };

            // Start a transaction to ensure all gets created or nothing does
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // First add the session to get a SessionId
                _context.Session.Add(session);
                await _context.SaveChangesAsync();

                // Create sets from template exercises
                foreach (var templateExercise in template.TemplateExercises.OrderBy(e => e.SequenceNum))
                {
                    // For each template set in this exercise, create a real set
                    foreach (var templateSet in templateExercise.TemplateSets.OrderBy(s => s.SequenceNum))
                    {
                        var set = new Set
                        {
                            SessionId = session.SessionId,
                            ExerciseTypeId = templateExercise.ExerciseTypeId,
                            SettypeId = templateSet.SettypeId,
                            Description = templateSet.Description,
                            Notes = templateSet.Notes,
                            NumberReps = templateSet.DefaultReps,
                            Weight = templateSet.DefaultWeight,
                            SequenceNum = templateSet.SequenceNum
                        };

                        _context.Set.Add(set);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Redirect to the new session
                return RedirectToPage("/Sessions/Edit", new { id = session.SessionId });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}