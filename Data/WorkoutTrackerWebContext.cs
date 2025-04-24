using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Models;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using WorkoutTrackerWeb.Models.Alerting;

namespace WorkoutTrackerWeb.Data
{
    public class WorkoutTrackerWebContext : DbContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private string _currentUserId => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        public WorkoutTrackerWebContext(DbContextOptions<WorkoutTrackerWebContext> options, 
                                       IHttpContextAccessor httpContextAccessor = null)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public DbSet<WorkoutTrackerWeb.Models.User> User { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.Session> Session { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.ExerciseType> ExerciseType { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.Rep> Rep { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.Set> Set { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.Settype> Settype { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.Feedback> Feedback { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.HelpArticle> HelpArticle { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.HelpCategory> HelpCategory { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.GlossaryTerm> GlossaryTerm { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.LoginHistory> LoginHistory { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.ShareToken> ShareToken { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.PendingExerciseSelection> PendingExerciseSelection { get; set; } = default!;

        // Alerting system DbSets
        public DbSet<WorkoutTrackerWeb.Models.Alerting.AlertThreshold> AlertThreshold { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.Alerting.Alert> Alert { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.Alerting.AlertHistory> AlertHistory { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.Alerting.Notification> Notification { get; set; } = default!;

        // Helper method to get the current user's own User record
        public async Task<User> GetCurrentUserAsync()
        {
            if (string.IsNullOrEmpty(_currentUserId))
                return null;

            return await User.FirstOrDefaultAsync(u => u.IdentityUserId == _currentUserId);
        }

        // Filter data based on current user
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // No global query filters on User table - we need to see all users for admin purposes
            // But we'll add filters to child entities

            // Sessions are filtered by the current user
            modelBuilder.Entity<Session>()
                .HasQueryFilter(s => _currentUserId == null || s.User.IdentityUserId == _currentUserId);

            // Sets are directly filtered by the current user (via Session)
            modelBuilder.Entity<Set>()
                .HasQueryFilter(s => _currentUserId == null || s.Session.User.IdentityUserId == _currentUserId);

            // Reps are filtered by the current user (via Set -> Session)
            modelBuilder.Entity<Rep>()
                .HasQueryFilter(r => _currentUserId == null || r.Set.Session.User.IdentityUserId == _currentUserId);
                
            // Feedback is filtered by the current user (only see your own feedback unless admin)
            modelBuilder.Entity<Feedback>()
                .HasQueryFilter(f => _currentUserId == null || f.User == null || f.User.IdentityUserId == _currentUserId);

            // Configure cascade delete for Rep-Set relationship
            modelBuilder.Entity<Set>()
                .HasMany(s => s.Reps)
                .WithOne(r => r.Set)
                .HasForeignKey(r => r.SetsSetId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Configure the relationship between Set and ExerciseType
            modelBuilder.Entity<Set>()
                .HasOne(s => s.ExerciseType)
                .WithMany(e => e.Sets)
                .HasForeignKey(s => s.ExerciseTypeId);
                
            // Configure the relationship between Set and Session
            modelBuilder.Entity<Set>()
                .HasOne(s => s.Session)
                .WithMany(s => s.Sets)
                .HasForeignKey(s => s.SessionId);
                
            // Configure the relationship between Feedback and User
            modelBuilder.Entity<Feedback>()
                .HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure self-referencing many-to-many relationship for HelpArticle RelatedArticles
            modelBuilder.Entity<HelpArticle>()
                .HasMany(a => a.RelatedArticles)
                .WithMany()
                .UsingEntity(j => j.ToTable("HelpArticleRelatedArticles"));

            // Configure relationship between HelpArticle and HelpCategory
            modelBuilder.Entity<HelpArticle>()
                .HasOne(a => a.Category)
                .WithMany(c => c.Articles)
                .HasForeignKey(a => a.HelpCategoryId);

            // Configure self-referencing relationship for HelpCategory
            modelBuilder.Entity<HelpCategory>()
                .HasOne(c => c.ParentCategory)
                .WithMany(c => c.ChildCategories)
                .HasForeignKey(c => c.ParentCategoryId);

            // Configure self-referencing many-to-many relationship for GlossaryTerm RelatedTerms
            modelBuilder.Entity<GlossaryTerm>()
                .HasMany(t => t.RelatedTerms)
                .WithMany()
                .UsingEntity(j => j.ToTable("GlossaryTermRelatedTerms"));
                
            // Configure ShareToken relationships and query filter
            modelBuilder.Entity<ShareToken>()
                .HasOne(st => st.User)
                .WithMany()
                .HasForeignKey(st => st.UserId)
                .OnDelete(DeleteBehavior.Restrict);
                
            modelBuilder.Entity<ShareToken>()
                .HasOne(st => st.Session)
                .WithMany()
                .HasForeignKey(st => st.SessionId)
                .OnDelete(DeleteBehavior.SetNull);
                
            // ShareTokens are filtered by the current user (only see your own tokens unless admin)
            modelBuilder.Entity<ShareToken>()
                .HasQueryFilter(st => _currentUserId == null || st.User.IdentityUserId == _currentUserId);
                
            // Configure alerting system relationships
            modelBuilder.Entity<Alert>()
                .HasOne(a => a.AlertThreshold)
                .WithMany()
                .HasForeignKey(a => a.AlertThresholdId)
                .OnDelete(DeleteBehavior.Restrict);
                
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Alert)
                .WithMany()
                .HasForeignKey(n => n.AlertId)
                .OnDelete(DeleteBehavior.SetNull);
                
            // Configure indexes for alerting system
            modelBuilder.Entity<AlertThreshold>()
                .HasIndex(at => at.MetricName);
                
            modelBuilder.Entity<Alert>()
                .HasIndex(a => a.TriggeredAt);
                
            modelBuilder.Entity<AlertHistory>()
                .HasIndex(ah => ah.TriggeredAt);
                
            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.UserId, n.IsRead });
        }
    }
}
