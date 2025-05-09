using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;

namespace WorkoutTrackerWeb.Utilities
{
    /// <summary>
    /// A utility class that analyzes Entity Framework Core models to detect potential shadow property conflicts.
    /// This can be used during development to prevent the creation of duplicate shadow properties.
    /// </summary>
    public class ShadowPropertyAnalyzer
    {
        private readonly ILogger _logger;

        public ShadowPropertyAnalyzer(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Analyzes an Entity Framework Core model for potential shadow property conflicts.
        /// </summary>
        /// <param name="model">The EF Core model to analyze</param>
        /// <returns>A collection of detected shadow property conflicts</returns>
        public IEnumerable<ShadowPropertyConflict> AnalyzeModel(IModel model)
        {
            var conflicts = new List<ShadowPropertyConflict>();

            foreach (var entityType in model.GetEntityTypes())
            {
                // Get all navigation properties
                var navigationProperties = entityType.GetNavigations().ToList();

                // Group navigations by target entity type to find potential conflicts
                var navigationsByTarget = navigationProperties
                    .GroupBy(n => n.TargetEntityType.Name)
                    .Where(g => g.Count() > 1)
                    .ToList();

                foreach (var group in navigationsByTarget)
                {
                    string targetType = group.Key;
                    var navigations = group.ToList();

                    // Check if there are potential conflicts in this group
                    if (navigations.Count > 1)
                    {
                        // Check foreign key properties to identify conflicts
                        var foreignKeyProperties = navigations
                            .SelectMany(n => n.ForeignKey.Properties)
                            .ToList();

                        // Look for shadow properties with number suffixes (indicating auto-created shadow properties)
                        var shadowProperties = foreignKeyProperties
                            .Where(p => p.IsShadowProperty() && p.Name.EndsWith("Id1"))
                            .ToList();

                        if (shadowProperties.Any())
                        {
                            var conflict = new ShadowPropertyConflict
                            {
                                EntityType = entityType.Name,
                                TargetEntityType = targetType,
                                ConflictingNavigations = navigations.Select(n => n.Name).ToList(),
                                ShadowProperties = shadowProperties.Select(p => p.Name).ToList()
                            };

                            conflicts.Add(conflict);
                            
                            _logger.LogWarning(
                                "Shadow property conflict detected in {EntityType} with navigations to {TargetType}: " +
                                "Properties: {Properties}, Navigations: {Navigations}",
                                entityType.Name, 
                                targetType, 
                                string.Join(", ", conflict.ShadowProperties),
                                string.Join(", ", conflict.ConflictingNavigations)
                            );
                        }
                    }
                }
            }

            return conflicts;
        }

        /// <summary>
        /// Analyzes a DbContext for potential shadow property conflicts.
        /// </summary>
        /// <param name="context">The DbContext to analyze</param>
        /// <returns>A collection of detected shadow property conflicts</returns>
        public IEnumerable<ShadowPropertyConflict> AnalyzeContext(DbContext context)
        {
            return AnalyzeModel(context.Model);
        }
    }

    /// <summary>
    /// Represents a detected shadow property conflict in an EF Core model.
    /// </summary>
    public class ShadowPropertyConflict
    {
        /// <summary>
        /// The entity type containing the conflict
        /// </summary>
        public string EntityType { get; set; }

        /// <summary>
        /// The target entity type that multiple navigations point to
        /// </summary>
        public string TargetEntityType { get; set; }

        /// <summary>
        /// The names of the navigation properties involved in the conflict
        /// </summary>
        public List<string> ConflictingNavigations { get; set; }

        /// <summary>
        /// The shadow properties created by EF Core that conflict
        /// </summary>
        public List<string> ShadowProperties { get; set; }
    }
}