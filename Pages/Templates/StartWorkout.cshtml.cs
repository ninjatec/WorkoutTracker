using System;
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

        public WorkoutSession WorkoutSession { get; set; }
        public WorkoutTemplate WorkoutTemplate { get; set; }
        public string DefaultSessionName => $"{WorkoutTemplate?.Name} {DateTime.Now:MMM d, yyyy}";
        public int? TemplateId { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
                return NotFound();

            TemplateId = id;

            // Modify query to avoid loading problematic columns using a projection approach
            var template = await _context.WorkoutTemplate
                .AsNoTracking()
                .Where(wt => wt.WorkoutTemplateId == id)
                .Select(wt => new WorkoutTemplate
                {
                    WorkoutTemplateId = wt.WorkoutTemplateId,
                    Name = wt.Name,
                    Description = wt.Description,
                    Category = wt.Category,
                    TemplateExercises = wt.TemplateExercises.OrderBy(te => te.OrderIndex)
                        .Select(te => new WorkoutTemplateExercise
                        {
                            WorkoutTemplateExerciseId = te.WorkoutTemplateExerciseId,
                            WorkoutTemplateId = te.WorkoutTemplateId,
                            ExerciseTypeId = te.ExerciseTypeId,
                            OrderIndex = te.OrderIndex,
                            Notes = te.Notes,
                            ExerciseType = new ExerciseType
                            {
                                ExerciseTypeId = te.ExerciseType.ExerciseTypeId,
                                Name = te.ExerciseType.Name,
                                Description = te.ExerciseType.Description,
                                Type = te.ExerciseType.Type
                            },
                            TemplateSets = te.TemplateSets.OrderBy(ts => ts.SequenceNum)
                                .Select(ts => new WorkoutTemplateSet
                                {
                                    WorkoutTemplateSetId = ts.WorkoutTemplateSetId,
                                    WorkoutTemplateExerciseId = ts.WorkoutTemplateExerciseId,
                                    SequenceNum = ts.SequenceNum,
                                    DefaultReps = ts.DefaultReps,
                                    DefaultWeight = ts.DefaultWeight,
                                    Notes = ts.Notes
                                }).ToList()
                        }).ToList()
                })
                .FirstOrDefaultAsync();

            WorkoutTemplate = template;

            if (WorkoutTemplate == null)
                return NotFound();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int templateId, string sessionName, DateTime? sessionDate, string sessionNotes)
        {
            // Use projection approach to avoid loading problematic columns
            var template = await _context.WorkoutTemplate
                .AsNoTracking()
                .Where(wt => wt.WorkoutTemplateId == templateId)
                .Select(wt => new WorkoutTemplate
                {
                    WorkoutTemplateId = wt.WorkoutTemplateId,
                    Name = wt.Name,
                    Description = wt.Description,
                    Category = wt.Category,
                    TemplateExercises = wt.TemplateExercises.OrderBy(te => te.OrderIndex)
                        .Select(te => new WorkoutTemplateExercise
                        {
                            WorkoutTemplateExerciseId = te.WorkoutTemplateExerciseId,
                            WorkoutTemplateId = te.WorkoutTemplateId,
                            ExerciseTypeId = te.ExerciseTypeId,
                            OrderIndex = te.OrderIndex,
                            Notes = te.Notes,
                            ExerciseType = new ExerciseType
                            {
                                ExerciseTypeId = te.ExerciseType.ExerciseTypeId,
                                Name = te.ExerciseType.Name,
                                Description = te.ExerciseType.Description
                            },
                            TemplateSets = te.TemplateSets.OrderBy(ts => ts.SequenceNum)
                                .Select(ts => new WorkoutTemplateSet
                                {
                                    WorkoutTemplateSetId = ts.WorkoutTemplateSetId,
                                    WorkoutTemplateExerciseId = ts.WorkoutTemplateExerciseId,
                                    SequenceNum = ts.SequenceNum,
                                    DefaultReps = ts.DefaultReps,
                                    DefaultWeight = ts.DefaultWeight,
                                    Notes = ts.Notes
                                }).ToList()
                        }).ToList()
                })
                .FirstOrDefaultAsync();

            if (template == null)
                return NotFound();

            // Create new workout session from template
            var user = await _context.GetCurrentUserAsync();
            if (user == null)
                return RedirectToPage("/Account/Login");

            WorkoutSession = new WorkoutSession
            {
                Name = sessionName ?? $"{template.Name} {DateTime.Now:MMM d, yyyy}",
                Description = sessionNotes,
                StartDateTime = sessionDate ?? DateTime.UtcNow,
                UserId = user.UserId,
                WorkoutTemplateId = template.WorkoutTemplateId,
                Status = "In Progress"
            };

            _context.WorkoutSessions.Add(WorkoutSession);
            await _context.SaveChangesAsync();

            // Create exercises and sets from template
            foreach (var templateExercise in template.TemplateExercises.OrderBy(e => e.OrderIndex))
            {
                var workoutExercise = new WorkoutExercise
                {
                    WorkoutSessionId = WorkoutSession.WorkoutSessionId,
                    ExerciseTypeId = templateExercise.ExerciseTypeId,
                    SequenceNum = templateExercise.OrderIndex,
                    OrderIndex = templateExercise.OrderIndex,
                    Notes = templateExercise.Notes
                };

                _context.WorkoutExercises.Add(workoutExercise);
                await _context.SaveChangesAsync();

                foreach (var templateSet in templateExercise.TemplateSets.OrderBy(s => s.SequenceNum))
                {
                    var workoutSet = new WorkoutSet
                    {
                        WorkoutExerciseId = workoutExercise.WorkoutExerciseId,
                        SetNumber = templateSet.SequenceNum,
                        SequenceNum = templateSet.SequenceNum,
                        Reps = templateSet.DefaultReps,
                        Weight = templateSet.DefaultWeight,
                        Notes = templateSet.Notes
                    };

                    _context.WorkoutSets.Add(workoutSet);
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToPage("/WorkoutSessions/Edit", new { id = WorkoutSession.WorkoutSessionId });
        }
    }
}