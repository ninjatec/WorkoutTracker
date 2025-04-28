using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Data;

namespace WorkoutTrackerWeb.Models.Filters
{
    /// <summary>
    /// Standard model for template filtering functionality across the application
    /// </summary>
    public class TemplateFilterModel
    {
        /// <summary>
        /// Search term for filtering templates by name, description, or tags
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        /// <summary>
        /// Category filter
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string Category { get; set; }

        /// <summary>
        /// Whether to include public templates
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public bool IncludePublic { get; set; } = true;

        /// <summary>
        /// List of available categories for the dropdown
        /// </summary>
        public List<string> Categories { get; set; } = new List<string>();

        /// <summary>
        /// Loads categories for the filter dropdown
        /// </summary>
        public async Task LoadCategoriesAsync(WorkoutTrackerWebContext context, int? userId = null)
        {
            IQueryable<WorkoutTemplate> query = context.WorkoutTemplate.AsQueryable();
            
            if (userId.HasValue)
            {
                query = query.Where(t => t.UserId == userId || t.IsPublic);
            }
            
            Categories = await query
                .Where(t => !string.IsNullOrEmpty(t.Category))
                .Select(t => t.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }

        /// <summary>
        /// Applies template filters to a query
        /// </summary>
        public IQueryable<WorkoutTemplate> ApplyFilters(IQueryable<WorkoutTemplate> query, int? userId = null)
        {
            // Apply owner filter
            if (userId.HasValue)
            {
                query = IncludePublic
                    ? query.Where(t => t.UserId == userId || t.IsPublic)
                    : query.Where(t => t.UserId == userId);
            }

            // Apply search term filter
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                query = query.Where(t => 
                    t.Name.Contains(SearchTerm) || 
                    (t.Description != null && t.Description.Contains(SearchTerm)) ||
                    (t.Tags != null && t.Tags.Contains(SearchTerm)));
            }

            // Apply category filter
            if (!string.IsNullOrEmpty(Category))
            {
                query = query.Where(t => t.Category == Category);
            }

            return query;
        }
    }
}