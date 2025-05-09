using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging;

namespace WorkoutTrackerWeb.Utilities
{
    /// <summary>
    /// A utility class to validate migrations for shadow property conflicts before they are applied.
    /// Can be used with the dotnet ef migrations script or dotnet ef database update commands.
    /// </summary>
    public class MigrationShadowPropertyValidator
    {
        private readonly DbContext _context;
        private readonly ILogger _logger;

        public MigrationShadowPropertyValidator(DbContext context, ILogger logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Validate pending migrations for shadow property conflicts.
        /// </summary>
        /// <returns>True if no conflicts were found, false otherwise</returns>
        public bool ValidatePendingMigrations()
        {
            _logger.LogInformation("Validating pending migrations for shadow property conflicts...");
            
            // Get the migration assembly and history
            var migrator = _context.GetInfrastructure().GetRequiredService<IMigrator>();
            var migrationsAssembly = _context.GetService<IMigrationsAssembly>();
            var historyRepository = _context.GetService<IHistoryRepository>();
            
            // Get applied migrations
            var appliedMigrations = historyRepository.GetAppliedMigrations();
            
            // Get all defined migrations
            var definedMigrations = migrationsAssembly.Migrations;
            
            // Find pending migrations
            var pendingMigrations = definedMigrations
                .Where(m => !appliedMigrations.Contains(m.Key))
                .OrderBy(m => m.Value.GetType().GetCustomAttributes(typeof(MigrationAttribute), false).Cast<MigrationAttribute>().First().Id)
                .ToList();

            if (!pendingMigrations.Any())
            {
                _logger.LogInformation("No pending migrations found to validate.");
                return true;
            }

            _logger.LogInformation("Validating {Count} pending migrations for shadow property conflicts...", pendingMigrations.Count);

            // Analyze the model for potential shadow property issues
            var analyzer = new ShadowPropertyAnalyzer(_logger);
            var conflicts = analyzer.AnalyzeContext(_context).ToList();

            if (conflicts.Any())
            {
                _logger.LogError("Found {Count} shadow property conflicts that would be included in pending migrations:", conflicts.Count);
                
                foreach (var conflict in conflicts)
                {
                    _logger.LogError("Entity {EntityType} has conflicting navigations to {TargetType}: {Navigations}. " +
                                    "This will create shadow properties like {ShadowProps}.",
                        conflict.EntityType,
                        conflict.TargetEntityType,
                        string.Join(", ", conflict.ConflictingNavigations),
                        string.Join(", ", conflict.ShadowProperties));
                }
                
                return false;
            }

            _logger.LogInformation("Validation complete: No shadow property conflicts found in pending migrations.");
            return true;
        }

        /// <summary>
        /// Extend the IMigrator service with shadow property validation capabilities.
        /// </summary>
        /// <param name="migrator">The migrator service</param>
        /// <param name="context">The DbContext</param>
        /// <param name="logger">The logger</param>
        /// <returns>True if validation passes, false otherwise</returns>
        public static bool ValidateBeforeMigration(IMigrator migrator, DbContext context, ILogger logger)
        {
            var validator = new MigrationShadowPropertyValidator(context, logger);
            return validator.ValidatePendingMigrations();
        }
    }
}