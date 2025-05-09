using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Design;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using WorkoutTrackerWeb.Utilities;

namespace WorkoutTrackerWeb.Data
{
    /// <summary>
    /// Design-time services for Entity Framework Core to enhance the migration creation process.
    /// This class is automatically discovered by EF Core when running migrations from command-line.
    /// </summary>
    public class DbContextDesignTimeServices : IDesignTimeServices
    {
        /// <summary>
        /// Configure design-time services to add shadow property validation.
        /// </summary>
        /// <param name="serviceCollection">The service collection to configure.</param>
        public void ConfigureDesignTimeServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<IMigrationsScaffolder, ShadowPropertyValidatingMigrationsScaffolder>();
        }
    }

    /// <summary>
    /// A migrations scaffolder that validates for shadow property conflicts before generating migrations.
    /// </summary>
    public class ShadowPropertyValidatingMigrationsScaffolder : MigrationsScaffolder
    {
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Creates a new instance of the ShadowPropertyValidatingMigrationsScaffolder.
        /// </summary>
        /// <param name="dependencies">The migration scaffolding dependencies.</param>
        public ShadowPropertyValidatingMigrationsScaffolder(MigrationsScaffolderDependencies dependencies) 
            : base(dependencies)
        {
            _loggerFactory = dependencies.CurrentContext.GetService<ILoggerFactory>() 
                ?? new LoggerFactory();
        }

        /// <summary>
        /// Scaffolds a new migration by first validating for shadow property conflicts.
        /// </summary>
        /// <param name="migrationName">The name of the migration.</param>
        /// <param name="rootNamespace">The root namespace for the migration.</param>
        /// <param name="subNamespace">The sub-namespace for the migration.</param>
        /// <param name="language">The language to generate the migration in.</param>
        /// <returns>The scaffolded migration files.</returns>
        public override ScaffoldedMigration ScaffoldMigration(
            string migrationName,
            string rootNamespace,
            string subNamespace = null,
            string language = null)
        {
            // Get a logger that will write to the console
            var logger = _loggerFactory.CreateLogger<ShadowPropertyValidatingMigrationsScaffolder>();
            
            // Get current model snapshot
            var currentModel = base.ModelSnapshot?.Model;
            if (currentModel != null)
            {
                // Analyze for shadow property conflicts
                var analyzer = new ShadowPropertyAnalyzer(logger);
                var conflicts = analyzer.AnalyzeModel(currentModel);
                
                var hasErrors = false;
                foreach (var conflict in conflicts)
                {
                    hasErrors = true;
                    logger.LogError(
                        "Shadow property conflict detected in entity {EntityType} with navigations to {TargetType}: {Navigations}",
                        conflict.EntityType, 
                        conflict.TargetEntityType,
                        string.Join(", ", conflict.ConflictingNavigations));
                    
                    logger.LogError(
                        "This will create shadow property conflicts with names like: {ShadowProperties}",
                        string.Join(", ", conflict.ShadowProperties));
                }
                
                if (hasErrors)
                {
                    logger.LogError(
                        "Shadow property conflicts detected. Fix these conflicts in your entity configurations " +
                        "before generating migrations. See WorkoutTrackerWebContext.OnModelCreating for examples.");
                    
                    logger.LogError(
                        "Hint: Use .HasOne() and .WithMany() relationships with explicit foreign key configuration to avoid conflicts.");
                    
                    // Throw an exception to prevent the migration from being generated
                    throw new InvalidOperationException("Shadow property conflicts detected. Migration generation aborted.");
                }
            }
            
            // If no conflicts were found, proceed with scaffolding
            return base.ScaffoldMigration(migrationName, rootNamespace, subNamespace, language);
        }
    }
}