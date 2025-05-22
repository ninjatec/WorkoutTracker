using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Models.Identity;
using WorkoutTrackerWeb.Models.Logging;

namespace WorkoutTrackerWeb.Data
{
    /// <summary>
    /// LEGACY CLASS: This is a minimal implementation of the original ApplicationDbContext.
    /// It exists only to support the existing migration files and should not be used for new code.
    /// All DB operations should use WorkoutTrackerWebContext instead.
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<AppUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Minimal set of DbSet properties needed to satisfy migration dependencies
        public DbSet<AppVersion> Versions { get; set; }
        public DbSet<LogLevelSettings> LogLevelSettings { get; set; }
        public DbSet<LogLevelOverride> LogLevelOverrides { get; set; }
        public DbSet<WhitelistedIp> WhitelistedIps { get; set; }
        
        // Minimal configuration needed for migrations
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // This minimal implementation only exists to support legacy migrations
            // Actual entity configuration is in WorkoutTrackerWebContext
        }
    }
}