using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;

namespace WorkoutTrackerWeb.Pages.Templates
{
    [Authorize]
    public class StartWorkoutModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly IWebHostEnvironment _environment;

        public StartWorkoutModel(WorkoutTrackerWebContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public WorkoutTemplate WorkoutTemplate { get; set; } = default!;
        public string DefaultSessionName { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            // Use AsSplitQuery and AsNoTracking for better performance
            var workoutTemplate = await _context.WorkoutTemplate
                .Include(t => t.TemplateExercises.OrderBy(e => e.SequenceNum))
                .ThenInclude(e => e.ExerciseType)
                .Include(t => t.TemplateExercises)
                .ThenInclude(e => e.TemplateSets.OrderBy(s => s.SequenceNum))
                .ThenInclude(s => s.Settype)
                .AsNoTracking()
                .AsSplitQuery()
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
            // Validate template exists - use a more optimized query
            var template = await _context.WorkoutTemplate
                .Include(t => t.TemplateExercises.OrderBy(e => e.SequenceNum))
                .ThenInclude(e => e.ExerciseType)
                .Include(t => t.TemplateExercises)
                .ThenInclude(e => e.TemplateSets.OrderBy(s => s.SequenceNum))
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
                Sets = new List<Set>(),
                // Initialize NotMapped properties
                TotalVolume = 0,
                EstimatedCalories = 0
            };

            // Start a transaction to ensure all gets created or nothing does
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // First add the session to get a SessionId
                _context.Session.Add(session);
                await _context.SaveChangesAsync();

                // Create sets from template exercises in batch instead of one by one
                var newSets = new List<Set>();
                
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
                            SequenceNum = templateSet.SequenceNum,
                            // Initialize NotMapped properties
                            Volume = templateSet.DefaultWeight * templateSet.DefaultReps,
                            EstimatedCalories = 0
                        };

                        newSets.Add(set);
                    }
                }
                
                // Add all sets at once
                _context.Set.AddRange(newSets);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Redirect to the new session
                return RedirectToPage("/Sessions/Details", new { id = session.SessionId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Log the exception for debugging
                Console.WriteLine($"Error creating workout from template: {ex.Message}");
                throw;
            }
        }
    }
}