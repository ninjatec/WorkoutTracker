using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Data;

namespace WorkoutTrackerWeb.Pages.ExerciseTypes
{
    public class IndexModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;

        public IndexModel(WorkoutTrackerWebContext context)
        {
            _context = context;
        }

        public IList<ExerciseType> ExerciseTypes { get; set; }
        
        public string NameSort { get; set; }
        public string TypeSort { get; set; }
        public string MuscleSort { get; set; }
        public string CurrentFilter { get; set; }
        public string CurrentSort { get; set; }
        
        public int PageIndex { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;

        public async Task OnGetAsync(string sortOrder, string currentFilter, string searchString, int? pageIndex)
        {
            // Set up sorting options
            CurrentSort = sortOrder;
            NameSort = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            TypeSort = sortOrder == "type" ? "type_desc" : "type";
            MuscleSort = sortOrder == "muscle" ? "muscle_desc" : "muscle";
            
            // Handle search and paging
            if (searchString != null)
            {
                pageIndex = 1;
            }
            else
            {
                searchString = currentFilter;
            }
            
            CurrentFilter = searchString;
            
            // Create IQueryable for the ExerciseType
            IQueryable<ExerciseType> exerciseTypesIQ = _context.ExerciseType;
            
            // Apply filter if search string is provided, with null checks
            if (!string.IsNullOrEmpty(searchString))
            {
                exerciseTypesIQ = exerciseTypesIQ.Where(e => 
                    e.Name.Contains(searchString) ||
                    (e.Type != null && e.Type.Contains(searchString)) ||
                    (e.Muscle != null && e.Muscle.Contains(searchString)) ||
                    (e.Difficulty != null && e.Difficulty.Contains(searchString)) ||
                    (e.Equipment != null && e.Equipment.Contains(searchString))
                );
            }
            
            // Apply sorting with null handling
            exerciseTypesIQ = sortOrder switch
            {
                "name_desc" => exerciseTypesIQ.OrderByDescending(e => e.Name),
                "type" => exerciseTypesIQ.OrderBy(e => e.Type ?? string.Empty).ThenBy(e => e.Name),
                "type_desc" => exerciseTypesIQ.OrderByDescending(e => e.Type ?? string.Empty).ThenBy(e => e.Name),
                "muscle" => exerciseTypesIQ.OrderBy(e => e.Muscle ?? string.Empty).ThenBy(e => e.Name),
                "muscle_desc" => exerciseTypesIQ.OrderByDescending(e => e.Muscle ?? string.Empty).ThenBy(e => e.Name),
                _ => exerciseTypesIQ.OrderBy(e => e.Name),
            };
            
            // Set up pagination
            int pageSize = 20;
            int totalItems = await exerciseTypesIQ.CountAsync();
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            PageIndex = pageIndex ?? 1;
            
            // Ensure PageIndex is valid
            if (PageIndex < 1)
            {
                PageIndex = 1;
            }
            else if (PageIndex > TotalPages && TotalPages > 0)
            {
                PageIndex = TotalPages;
            }
            
            // Get the page of data
            ExerciseTypes = await exerciseTypesIQ
                .Skip((PageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
    }
}