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
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            var builder = new DbContextOptionsBuilder<WorkoutTrackerWebContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            
            builder.UseSqlServer(connectionString);

            return new WorkoutTrackerWebContext(builder.Options);
        }
    }
}