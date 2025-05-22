using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace WorkoutTrackerWeb.Data
{
    public class WorkoutTrackerWebContextFactory : IDesignTimeDbContextFactory<WorkoutTrackerWebContext>
    {
        public WorkoutTrackerWebContext CreateDbContext(string[] args)
        {
            // Get the project's user secrets ID from the project file
            var userSecretsId = "aspnet-WorkoutTrackerWeb-9e87d476-9b38-425e-8de1-18d836571a09";

            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                // Explicitly load user secrets for EF migrations
                .AddUserSecrets(userSecretsId)
                .AddEnvironmentVariables()
                .Build();

            var builder = new DbContextOptionsBuilder<WorkoutTrackerWebContext>();
            var connectionString = configuration.GetConnectionString("WorkoutTrackerWebContext");
            
            // Fall back to DefaultConnection if WorkoutTrackerWebContext connection string is not found
            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = configuration.GetConnectionString("DefaultConnection");
            }
            
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    "No connection string found. Please ensure ConnectionStrings:WorkoutTrackerWebContext or ConnectionStrings:DefaultConnection is set in user secrets or configuration."
                );
            }
            
            builder.UseSqlServer(connectionString, options => {
                // Set a longer command timeout for migrations (in seconds)
                options.CommandTimeout(300);
                // Enable retries for transient errors during migrations
                options.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null
                );
            });

            return new WorkoutTrackerWebContext(builder.Options);
        }
    }
}