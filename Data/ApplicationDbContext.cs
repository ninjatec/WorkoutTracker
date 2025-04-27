using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Models.Logging;
using WorkoutTrackerWeb.Models.Identity;
using WorkoutTrackerWeb.Models.Coaching;

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
    
    public DbSet<CoachClientRelationship> CoachClientRelationships { get; set; }
    
    public DbSet<CoachClientPermission> CoachClientPermissions { get; set; }
    
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
            
        // Configure Coach/Client relationships
        builder.Entity<CoachClientRelationship>()
            .HasOne(r => r.Coach)
            .WithMany(u => u.CoachRelationships)
            .HasForeignKey(r => r.CoachId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.Entity<CoachClientRelationship>()
            .HasOne(r => r.Client)
            .WithMany(u => u.ClientRelationships)
            .HasForeignKey(r => r.ClientId)
            .OnDelete(DeleteBehavior.Restrict);
            
        // Configure CoachClientPermission relationship
        builder.Entity<CoachClientPermission>()
            .HasOne(p => p.Relationship)
            .WithOne(r => r.Permissions)
            .HasForeignKey<CoachClientPermission>(p => p.CoachClientRelationshipId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
