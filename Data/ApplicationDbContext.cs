using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Models.Logging;
using WorkoutTrackerWeb.Models.Identity;

namespace WorkoutTrackerWeb.Data;

public class ApplicationDbContext : IdentityDbContext<AppUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<AppVersion> Versions { get; set; }
    
    public DbSet<LogLevelSettings> LogLevelSettings { get; set; }
    
    public DbSet<LogLevelOverride> LogLevelOverrides { get; set; }
    
    public DbSet<WhitelistedIp> WhitelistedIps { get; set; }
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // Configure relationships for log level settings
        builder.Entity<LogLevelSettings>()
            .HasMany(s => s.Overrides)
            .WithOne(o => o.LogLevelSettings)
            .HasForeignKey(o => o.LogLevelSettingsId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Configure WhitelistedIp
        builder.Entity<WhitelistedIp>()
            .HasIndex(w => w.IpAddress)
            .IsUnique();
    }
}
