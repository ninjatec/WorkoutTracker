using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.OutputCaching;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;

namespace WorkoutTrackerWeb.Pages.Templates
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly IOutputCacheStore _cacheStore;

        public EditModel(WorkoutTrackerWebContext context, IOutputCacheStore cacheStore)
        {
            _context = context;
            _cacheStore = cacheStore;
        }

        [BindProperty]
        public WorkoutTemplate WorkoutTemplate { get; set; } = default!;
        
        public List<ExerciseType> ExerciseTypes { get; set; } = new List<ExerciseType>();
        public List<Settype> SetTypes { get; set; } = new List<Settype>();
        public List<string> Categories { get; set; } = new List<string>();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var workoutTemplate = await _context.WorkoutTemplate
                .Include(t => t.TemplateExercises)
                .ThenInclude(e => e.ExerciseType)
                .Include(t => t.TemplateExercises)
                .ThenInclude(e => e.TemplateSets)
                .ThenInclude(s => s.Settype)
                .FirstOrDefaultAsync(t => t.WorkoutTemplateId == id);

            if (workoutTemplate == null)
            {
                return NotFound();
            }

            // Verify that the template belongs to the current user
            var currentUser = await _context.GetCurrentUserAsync();
            if (currentUser == null || workoutTemplate.UserId != currentUser.UserId)
            {
                return Forbid();
            }

            WorkoutTemplate = workoutTemplate;
            
            // Get all exercise types for the selection dropdowns
            ExerciseTypes = await _context.ExerciseType
                .OrderBy(e => e.Name)
                .ToListAsync();
                
            // Get all set types for the selection dropdowns
            SetTypes = await _context.Settype
                .OrderBy(s => s.Name)
                .ToListAsync();
                
            // Get existing categories for autocomplete
            Categories = await _context.WorkoutTemplate
                .Where(t => !string.IsNullOrEmpty(t.Category))
                .Select(t => t.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Get the current template from the database to validate ownership
            var currentUser = await _context.GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToPage("/Account/Login");
            }

            var existingTemplate = await _context.WorkoutTemplate
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.WorkoutTemplateId == WorkoutTemplate.WorkoutTemplateId);

            if (existingTemplate == null)
            {
                return NotFound();
            }

            // Verify ownership
            if (existingTemplate.UserId != currentUser.UserId)
            {
                return Forbid();
            }

            // Validate model
            if (!ModelState.IsValid)
            {
                // Reload required data for the page
                ExerciseTypes = await _context.ExerciseType.OrderBy(e => e.Name).ToListAsync();
                SetTypes = await _context.Settype.OrderBy(s => s.Name).ToListAsync();
                Categories = await _context.WorkoutTemplate
                    .Where(t => !string.IsNullOrEmpty(t.Category))
                    .Select(t => t.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();
                return Page();
            }

            // Sanitize tags input
            if (!string.IsNullOrEmpty(WorkoutTemplate.Tags))
            {
                WorkoutTemplate.Tags = string.Join(", ", 
                    WorkoutTemplate.Tags
                        .Split(',')
                        .Select(tag => tag.Trim())
                        .Where(tag => !string.IsNullOrEmpty(tag)));
            }

            // Update timestamp
            WorkoutTemplate.LastModifiedDate = DateTime.Now;

            // Use defensive loading to avoid unauthorized changes to relationships
            var templateToUpdate = await _context.WorkoutTemplate
                .Include(t => t.TemplateExercises)
                .ThenInclude(e => e.TemplateSets)
                .FirstOrDefaultAsync(t => t.WorkoutTemplateId == WorkoutTemplate.WorkoutTemplateId);

            if (templateToUpdate != null)
            {
                // Update only the allowed fields, preserving relationships
                templateToUpdate.Name = WorkoutTemplate.Name;
                templateToUpdate.Description = WorkoutTemplate.Description;
                templateToUpdate.Category = WorkoutTemplate.Category;
                templateToUpdate.Tags = WorkoutTemplate.Tags;
                templateToUpdate.IsPublic = WorkoutTemplate.IsPublic;
                templateToUpdate.LastModifiedDate = WorkoutTemplate.LastModifiedDate;

                try
                {
                    await _context.SaveChangesAsync();
                    
                    // Invalidate output cache for this template
                    await _cacheStore.EvictByTagAsync($"template-{WorkoutTemplate.WorkoutTemplateId}", default);
                    
                    return RedirectToPage("./Details", new { id = WorkoutTemplate.WorkoutTemplateId });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TemplateExists(WorkoutTemplate.WorkoutTemplateId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return NotFound();
        }

        public async Task<IActionResult> OnPostAddExerciseAsync(int templateId, int exerciseTypeId, int sequenceNum, string notes)
        {
            // Validate ownership
            var currentUser = await _context.GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToPage("/Account/Login");
            }

            var template = await _context.WorkoutTemplate
                .FirstOrDefaultAsync(t => t.WorkoutTemplateId == templateId);

            if (template == null || template.UserId != currentUser.UserId)
            {
                return Forbid();
            }

            // Create new exercise
            var exercise = new WorkoutTemplateExercise
            {
                WorkoutTemplateId = templateId,
                ExerciseTypeId = exerciseTypeId,
                SequenceNum = sequenceNum,
                Notes = notes ?? string.Empty
            };

            _context.WorkoutTemplateExercise.Add(exercise);
            
            // Update template modification date
            template.LastModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            
            // Invalidate output cache for this template
            await _cacheStore.EvictByTagAsync($"template-{templateId}", default);

            return RedirectToPage("./Edit", new { id = templateId });
        }

        public async Task<IActionResult> OnPostDeleteExerciseAsync(int templateId, int exerciseId)
        {
            // Validate ownership
            var currentUser = await _context.GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToPage("/Account/Login");
            }

            var template = await _context.WorkoutTemplate
                .FirstOrDefaultAsync(t => t.WorkoutTemplateId == templateId);

            if (template == null || template.UserId != currentUser.UserId)
            {
                return Forbid();
            }

            // Find exercise and its associated sets
            var exercise = await _context.WorkoutTemplateExercise
                .Include(e => e.TemplateSets)
                .FirstOrDefaultAsync(e => e.WorkoutTemplateExerciseId == exerciseId && e.WorkoutTemplateId == templateId);

            if (exercise == null)
            {
                return NotFound();
            }

            // Remove sets first, then the exercise
            _context.WorkoutTemplateSet.RemoveRange(exercise.TemplateSets);
            _context.WorkoutTemplateExercise.Remove(exercise);
            
            // Update template modification date
            template.LastModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            
            // Invalidate output cache for this template
            await _cacheStore.EvictByTagAsync($"template-{templateId}", default);

            return RedirectToPage("./Edit", new { id = templateId });
        }

        public async Task<IActionResult> OnPostAddSetAsync(int templateId, int exerciseId, int settypeId, 
            int defaultReps, decimal defaultWeight, int sequenceNum, string description, string notes)
        {
            // Validate ownership
            var currentUser = await _context.GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToPage("/Account/Login");
            }

            var template = await _context.WorkoutTemplate
                .FirstOrDefaultAsync(t => t.WorkoutTemplateId == templateId);

            if (template == null || template.UserId != currentUser.UserId)
            {
                return Forbid();
            }

            // Verify exercise belongs to the template
            var exercise = await _context.WorkoutTemplateExercise
                .FirstOrDefaultAsync(e => e.WorkoutTemplateExerciseId == exerciseId && e.WorkoutTemplateId == templateId);

            if (exercise == null)
            {
                return NotFound();
            }

            // Create new set
            var set = new WorkoutTemplateSet
            {
                WorkoutTemplateExerciseId = exerciseId,
                SettypeId = settypeId,
                DefaultReps = defaultReps,
                DefaultWeight = defaultWeight,
                SequenceNum = sequenceNum,
                Description = description ?? string.Empty,
                Notes = notes ?? string.Empty
            };

            _context.WorkoutTemplateSet.Add(set);
            
            // Update template modification date
            template.LastModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            
            // Invalidate output cache for this template
            await _cacheStore.EvictByTagAsync($"template-{templateId}", default);

            return RedirectToPage("./Edit", new { id = templateId });
        }

        public async Task<IActionResult> OnPostDeleteSetAsync(int templateId, int setId)
        {
            // Validate ownership
            var currentUser = await _context.GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToPage("/Account/Login");
            }

            var template = await _context.WorkoutTemplate
                .FirstOrDefaultAsync(t => t.WorkoutTemplateId == templateId);

            if (template == null || template.UserId != currentUser.UserId)
            {
                return Forbid();
            }

            // Find set and verify it belongs to the template
            var set = await _context.WorkoutTemplateSet
                .Include(s => s.WorkoutTemplateExercise)
                .FirstOrDefaultAsync(s => s.WorkoutTemplateSetId == setId && 
                                        s.WorkoutTemplateExercise.WorkoutTemplateId == templateId);

            if (set == null)
            {
                return NotFound();
            }

            _context.WorkoutTemplateSet.Remove(set);
            
            // Update template modification date
            template.LastModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            
            // Invalidate output cache for this template
            await _cacheStore.EvictByTagAsync($"template-{templateId}", default);

            return RedirectToPage("./Edit", new { id = templateId });
        }

        public async Task<IActionResult> OnPostCloneSetAsync(int templateId, int setId)
        {
            // Validate ownership
            var currentUser = await _context.GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToPage("/Account/Login");
            }

            var template = await _context.WorkoutTemplate
                .FirstOrDefaultAsync(t => t.WorkoutTemplateId == templateId);

            if (template == null || template.UserId != currentUser.UserId)
            {
                return Forbid();
            }

            // Find set to clone and verify it belongs to the template
            var setToClone = await _context.WorkoutTemplateSet
                .Include(s => s.WorkoutTemplateExercise)
                .FirstOrDefaultAsync(s => s.WorkoutTemplateSetId == setId && 
                                        s.WorkoutTemplateExercise.WorkoutTemplateId == templateId);

            if (setToClone == null)
            {
                return NotFound();
            }

            // Get the highest sequence number in this exercise's sets
            var maxSequenceNum = await _context.WorkoutTemplateSet
                .Where(s => s.WorkoutTemplateExerciseId == setToClone.WorkoutTemplateExerciseId)
                .Select(s => s.SequenceNum)
                .DefaultIfEmpty(0)
                .MaxAsync();

            // Create a clone with next sequence number
            var clonedSet = new WorkoutTemplateSet
            {
                WorkoutTemplateExerciseId = setToClone.WorkoutTemplateExerciseId,
                SettypeId = setToClone.SettypeId,
                DefaultReps = setToClone.DefaultReps,
                DefaultWeight = setToClone.DefaultWeight,
                SequenceNum = maxSequenceNum + 1,
                Description = string.IsNullOrEmpty(setToClone.Description) 
                    ? "Clone of Set #" + setToClone.SequenceNum
                    : setToClone.Description + " (Clone)",
                Notes = setToClone.Notes
            };

            _context.WorkoutTemplateSet.Add(clonedSet);
            
            // Update template modification date
            template.LastModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            
            // Invalidate output cache for this template
            await _cacheStore.EvictByTagAsync($"template-{templateId}", default);

            return RedirectToPage("./Edit", new { id = templateId });
        }

        public async Task<IActionResult> OnPostEditSetAsync(int templateId, int setId, int settypeId, 
            int defaultReps, decimal defaultWeight, int sequenceNum, string description, string notes)
        {
            // Validate ownership
            var currentUser = await _context.GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToPage("/Account/Login");
            }

            var template = await _context.WorkoutTemplate
                .FirstOrDefaultAsync(t => t.WorkoutTemplateId == templateId);

            if (template == null || template.UserId != currentUser.UserId)
            {
                return Forbid();
            }

            // Find the set and verify it belongs to the template
            var setToUpdate = await _context.WorkoutTemplateSet
                .Include(s => s.WorkoutTemplateExercise)
                .FirstOrDefaultAsync(s => s.WorkoutTemplateSetId == setId && 
                                      s.WorkoutTemplateExercise.WorkoutTemplateId == templateId);

            if (setToUpdate == null)
            {
                return NotFound();
            }

            // Update set properties
            setToUpdate.SettypeId = settypeId;
            setToUpdate.DefaultReps = defaultReps;
            setToUpdate.DefaultWeight = defaultWeight;
            setToUpdate.SequenceNum = sequenceNum;
            setToUpdate.Description = description ?? string.Empty;
            setToUpdate.Notes = notes ?? string.Empty;
            
            // Update template modification date
            template.LastModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            
            // Invalidate output cache for this template
            await _cacheStore.EvictByTagAsync($"template-{templateId}", default);

            return RedirectToPage("./Edit", new { id = templateId });
        }

        private bool TemplateExists(int id)
        {
            return _context.WorkoutTemplate.Any(e => e.WorkoutTemplateId == id);
        }
    }
}