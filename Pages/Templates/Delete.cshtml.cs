using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.OutputCaching;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;

namespace WorkoutTrackerWeb.Pages.Templates
{
    [Authorize]
    public class DeleteModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly IOutputCacheStore _cacheStore;

        public DeleteModel(WorkoutTrackerWebContext context, IOutputCacheStore cacheStore)
        {
            _context = context;
            _cacheStore = cacheStore;
        }

        [BindProperty]
        public WorkoutTemplate WorkoutTemplate { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var workoutTemplate = await _context.WorkoutTemplate
                .Include(t => t.TemplateExercises)
                .AsNoTracking()
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
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            // Validate that template exists and belongs to current user
            var currentUser = await _context.GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToPage("/Account/Login");
            }

            var template = await _context.WorkoutTemplate
                .Include(t => t.TemplateExercises)
                .ThenInclude(e => e.TemplateSets)
                .FirstOrDefaultAsync(t => t.WorkoutTemplateId == id);

            if (template == null)
            {
                return NotFound();
            }

            if (template.UserId != currentUser.UserId)
            {
                return Forbid();
            }

            // Use the execution strategy to handle transactional operations
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () => 
            {
                // Start a transaction for consistent deletion within the strategy
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Remove sets first, then exercises, then the template
                    foreach (var exercise in template.TemplateExercises)
                    {
                        _context.WorkoutTemplateSet.RemoveRange(exercise.TemplateSets);
                    }

                    _context.WorkoutTemplateExercise.RemoveRange(template.TemplateExercises);
                    _context.WorkoutTemplate.Remove(template);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch
                {
                    // Transaction will automatically be rolled back when the using block exits
                    throw;
                }
            });

            // Invalidate related caches (outside the transaction)
            await _cacheStore.EvictByTagAsync($"template-{id}", default);
            await _cacheStore.EvictByTagAsync("templates", default);

            return RedirectToPage("./Index");
        }
    }
}