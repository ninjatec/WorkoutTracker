using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Attributes;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Models.Coaching;

namespace WorkoutTrackerWeb.Areas.Coach.Pages.Templates
{
    [CoachAuthorize]
    public class CreateModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;

        public CreateModel(WorkoutTrackerWebContext context)
        {
            _context = context;
        }

        [BindProperty]
        public WorkoutTemplate Template { get; set; } = new WorkoutTemplate();

        [BindProperty]
        public List<WorkoutTemplateExercise> Exercises { get; set; } = new List<WorkoutTemplateExercise>();

        [BindProperty]
        public List<List<WorkoutTemplateSet>> ExerciseSets { get; set; } = new List<List<WorkoutTemplateSet>>();

        public SelectList ExerciseTypes { get; set; }
        public SelectList SetTypes { get; set; }
        public List<string> MuscleGroups { get; set; }
        public List<string> EquipmentTypes { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Get the current user
            var user = await _context.GetCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized();
            }

            // Load exercise types for dropdown
            ExerciseTypes = new SelectList(await _context.ExerciseType
                .OrderBy(e => e.Name)
                .Select(e => new { e.ExerciseTypeId, e.Name })
                .ToListAsync(), "ExerciseTypeId", "Name");

            // Load set types for dropdown
            SetTypes = new SelectList(await _context.Settype
                .OrderBy(s => s.Name)
                .Select(s => new { s.SettypeId, s.Name })
                .ToListAsync(), "SettypeId", "Name");

            // Load muscle groups for filtering
            MuscleGroups = await _context.ExerciseType
                .Where(e => !string.IsNullOrEmpty(e.PrimaryMuscles))
                .Select(e => e.PrimaryMuscles)
                .Distinct()
                .OrderBy(m => m)
                .ToListAsync();

            // Load equipment types for filtering
            EquipmentTypes = await _context.ExerciseType
                .Where(e => !string.IsNullOrEmpty(e.Equipment))
                .Select(e => e.Equipment)
                .Distinct()
                .OrderBy(e => e)
                .ToListAsync();

            // Initialize empty template with default values
            Template.UserId = user.UserId;
            Template.CreatedDate = DateTime.Now;
            Template.LastModifiedDate = DateTime.Now;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Get the current user
            var user = await _context.GetCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized();
            }

            // Ensure the template belongs to the current user
            Template.UserId = user.UserId;
            Template.CreatedDate = DateTime.Now;
            Template.LastModifiedDate = DateTime.Now;

            // Check if the model is valid
            if (!ModelState.IsValid)
            {
                await LoadSelectLists();
                return Page();
            }

            // Begin transaction
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Add and save the template
                _context.WorkoutTemplate.Add(Template);
                await _context.SaveChangesAsync();

                // Process exercises and their sets
                for (int i = 0; i < Exercises.Count; i++)
                {
                    var exercise = Exercises[i];
                    
                    // Skip if no exercise type is selected
                    if (exercise.ExerciseTypeId <= 0)
                        continue;
                        
                    // Set template ID and save
                    exercise.WorkoutTemplateId = Template.WorkoutTemplateId;
                    _context.WorkoutTemplateExercise.Add(exercise);
                    await _context.SaveChangesAsync();
                    
                    // Process sets for this exercise
                    if (i < ExerciseSets.Count)
                    {
                        foreach (var set in ExerciseSets[i])
                        {
                            // Skip if no set type is selected
                            if (set.SettypeId <= 0)
                                continue;
                                
                            // Set exercise ID and save
                            set.WorkoutTemplateExerciseId = exercise.WorkoutTemplateExerciseId;
                            _context.WorkoutTemplateSet.Add(set);
                        }
                        await _context.SaveChangesAsync();
                    }
                }

                // Commit transaction
                await transaction.CommitAsync();
                
                // Redirect to template list
                return RedirectToPage("./Index");
            }
            catch (Exception)
            {
                // Rollback transaction on error
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty, "An error occurred while saving the template. Please try again.");
                await LoadSelectLists();
                return Page();
            }
        }

        [HttpGet]
        public async Task<IActionResult> OnGetFilterExercisesAsync(string muscleGroup, string equipment, string searchTerm)
        {
            // Start with all exercises
            var query = _context.ExerciseType.AsQueryable();
            
            // Apply muscle group filter
            if (!string.IsNullOrEmpty(muscleGroup))
            {
                query = query.Where(e => e.PrimaryMuscles == muscleGroup || 
                                         e.SecondaryMuscles.Contains(muscleGroup));
            }
            
            // Apply equipment filter
            if (!string.IsNullOrEmpty(equipment))
            {
                query = query.Where(e => e.Equipment == equipment);
            }
            
            // Apply search term
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(e => e.Name.Contains(searchTerm) || 
                                         (e.Description != null && e.Description.Contains(searchTerm)));
            }
            
            // Get filtered results
            var filteredExercises = await query
                .OrderBy(e => e.Name)
                .Select(e => new { value = e.ExerciseTypeId.ToString(), text = e.Name })
                .ToListAsync();
                
            return new JsonResult(filteredExercises);
        }

        private async Task LoadSelectLists()
        {
            // Load exercise types for dropdown
            ExerciseTypes = new SelectList(await _context.ExerciseType
                .OrderBy(e => e.Name)
                .Select(e => new { e.ExerciseTypeId, e.Name })
                .ToListAsync(), "ExerciseTypeId", "Name");

            // Load set types for dropdown
            SetTypes = new SelectList(await _context.Settype
                .OrderBy(s => s.Name)
                .Select(s => new { s.SettypeId, s.Name })
                .ToListAsync(), "SettypeId", "Name");

            // Load muscle groups for filtering
            MuscleGroups = await _context.ExerciseType
                .Where(e => !string.IsNullOrEmpty(e.PrimaryMuscles))
                .Select(e => e.PrimaryMuscles)
                .Distinct()
                .OrderBy(m => m)
                .ToListAsync();

            // Load equipment types for filtering
            EquipmentTypes = await _context.ExerciseType
                .Where(e => !string.IsNullOrEmpty(e.Equipment))
                .Select(e => e.Equipment)
                .Distinct()
                .OrderBy(e => e)
                .ToListAsync();
        }
    }
}