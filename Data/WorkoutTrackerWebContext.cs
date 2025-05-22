using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Models;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using WorkoutTrackerWeb.Models.Alerting;
using WorkoutTrackerWeb.Models.Blog;
using WorkoutTrackerWeb.Models.Coaching;
using WorkoutTrackerWeb.Models.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using WorkoutTrackerWeb.Models.Logging;

namespace WorkoutTrackerWeb.Data
{
    public class WorkoutTrackerWebContext : IdentityDbContext<AppUser>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        
        // Fixed property to handle null HttpContext scenarios safely
        private string _currentUserId => 
            _httpContextAccessor?.HttpContext?.User?.Identity?.IsAuthenticated == true ? 
            _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) : 
            null;

        public WorkoutTrackerWebContext(DbContextOptions<WorkoutTrackerWebContext> options, 
                                       IHttpContextAccessor httpContextAccessor = null)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // Models from ApplicationDbContext that need to be added
        public DbSet<AppVersion> Versions { get; set; } = default!;
        public DbSet<LogLevelSettings> LogLevelSettings { get; set; } = default!;
        public DbSet<LogLevelOverride> LogLevelOverrides { get; set; } = default!;
        public DbSet<WhitelistedIp> WhitelistedIps { get; set; } = default!;

        public DbSet<WorkoutTrackerWeb.Models.User> User { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.ExerciseType> ExerciseType { get; set; } = default!;
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
        public DbSet<WorkoutTrackerWeb.Models.Alerting.Notification> Notification { get; set; } = default!;        // Workout template DbSets
        public DbSet<WorkoutTrackerWeb.Models.WorkoutTemplate> WorkoutTemplate { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.WorkoutTemplateExercise> WorkoutTemplateExercise { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.WorkoutTemplateSet> WorkoutTemplateSet { get; set; } = default!;
        
        // Blog DbSets
        public DbSet<WorkoutTrackerWeb.Models.Blog.BlogPost> BlogPost { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.Blog.BlogCategory> BlogCategory { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.Blog.BlogTag> BlogTag { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.Blog.BlogPostCategory> BlogPostCategory { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.Blog.BlogPostTag> BlogPostTag { get; set; } = default!;

        // Coaching system DbSets
        public DbSet<WorkoutTrackerWeb.Models.Coaching.CoachClientRelationship> CoachClientRelationships { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.Coaching.CoachClientPermission> CoachClientPermissions { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.Coaching.ClientGroup> ClientGroups { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.Coaching.ClientGroupMember> ClientGroupMembers { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.Coaching.CoachNote> CoachNotes { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.Coaching.ClientGoal> ClientGoals { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.Coaching.GoalMilestone> GoalMilestones { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.Coaching.CoachClientMessage> CoachClientMessages { get; set; } = default!;
        
        // New workout programming DbSets
        public DbSet<WorkoutTrackerWeb.Models.Coaching.TemplateAssignment> TemplateAssignments { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.Coaching.WorkoutSchedule> WorkoutSchedules { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.Coaching.WorkoutFeedback> WorkoutFeedbacks { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.Coaching.ExerciseFeedback> ExerciseFeedbacks { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.Coaching.ProgressionRule> ProgressionRules { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.Coaching.ProgressionHistory> ProgressionHistories { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.Coaching.ExerciseSubstitution> ExerciseSubstitutions { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.Coaching.ClientExerciseExclusion> ClientExerciseExclusions { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.Coaching.ClientEquipment> ClientEquipments { get; set; } = default!;

        // New workout tracking DbSets
        public DbSet<WorkoutTrackerWeb.Models.WorkoutSession> WorkoutSessions { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.WorkoutExercise> WorkoutExercises { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.WorkoutSet> WorkoutSets { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.Rep> Rep { get; set; } = default!;

        // New DbSets for coach dashboard integration
        public DbSet<WorkoutTrackerWeb.Models.Coaching.GoalFeedback> GoalFeedback { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.Coaching.ClientActivity> ClientActivities { get; set; } = default!;

        // Helper method to get the current user's own User record
        public async Task<User> GetCurrentUserAsync()
        {
            if (string.IsNullOrEmpty(_currentUserId))
                return null;

            // Add OrderBy to ensure consistent results
            return await User
                .Where(u => u.IdentityUserId == _currentUserId)
                .OrderBy(u => u.UserId)
                .FirstOrDefaultAsync();
        }

        // Filter data based on current user
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Add index on UserName to enforce uniqueness (from ApplicationDbContext)
            modelBuilder.Entity<AppUser>()
                .HasIndex(u => u.UserName)
                .IsUnique();

            // Configure relationships for log level settings (from ApplicationDbContext)
            modelBuilder.Entity<LogLevelSettings>()
                .HasMany(s => s.Overrides)
                .WithOne(o => o.LogLevelSettings)
                .HasForeignKey(o => o.LogLevelSettingsId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure WhitelistedIp with unique index (from ApplicationDbContext)
            modelBuilder.Entity<WhitelistedIp>()
                .HasIndex(w => w.IpAddress)
                .IsUnique();

            // Configure precision and scale for decimal properties to avoid truncation warnings
            modelBuilder.Entity<ClientGoal>()
                .Property(g => g.CurrentValue)
                .HasPrecision(10, 2);
                
            modelBuilder.Entity<ClientGoal>()
                .Property(g => g.StartValue)
                .HasPrecision(10, 2);
                
            modelBuilder.Entity<ClientGoal>()
                .Property(g => g.TargetValue)
                .HasPrecision(10, 2);
                
            modelBuilder.Entity<ProgressionRule>()
                .Property(p => p.MaximumValue)
                .HasPrecision(10, 2);
                
            modelBuilder.Entity<WorkoutSession>()
                .Property(w => w.CaloriesBurned)
                .HasPrecision(10, 2);
            
            // *** RELATIONSHIP CONFIGURATIONS TO FIX SHADOW PROPERTY ISSUES ***

            // 1. Fix CoachClientRelationship <-> AppUser relationships to prevent AppUserId1 shadow property
            // Define relationships with AppUser explicitly using correct property names
            modelBuilder.Entity<CoachClientRelationship>()
                .HasOne(r => r.Coach)
                .WithMany(u => u.CoachRelationships)
                .HasForeignKey(r => r.CoachId)
                .OnDelete(DeleteBehavior.NoAction);
                
            modelBuilder.Entity<CoachClientRelationship>()
                .HasOne(r => r.Client)
                .WithMany(u => u.ClientRelationships)
                .HasForeignKey(r => r.ClientId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false); // Allow null for invited users who are not registered yet
            
            // 2. Fix WorkoutFeedback <-> WorkoutSession relationship to prevent WorkoutSessionId1 shadow property
            modelBuilder.Entity<WorkoutSession>()
                .HasOne(ws => ws.WorkoutFeedback)
                .WithOne(wf => wf.WorkoutSession)
                .HasForeignKey<WorkoutFeedback>(wf => wf.WorkoutSessionId);

            // Configure WorkoutExercise <-> ExerciseType relationship
            modelBuilder.Entity<WorkoutExercise>()
                .HasOne(we => we.ExerciseType)
                .WithMany(et => et.WorkoutExercises)
                .HasForeignKey(we => we.ExerciseTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Property Access Mode for navigation property
            modelBuilder.Entity<WorkoutExercise>()
                .Navigation(we => we.ExerciseType)
                .UsePropertyAccessMode(PropertyAccessMode.Property);
            
            // Configure Equipment relationship separately
            modelBuilder.Entity<WorkoutExercise>()
                .HasOne(we => we.Equipment)
                .WithMany()
                .HasForeignKey(we => we.EquipmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // 3. Fix ProgressionRule <-> ProgressionHistory relationship
            modelBuilder.Entity<ProgressionRule>()
                .HasMany(pr => pr.ProgressionHistory)
                .WithOne(ph => ph.ProgressionRule)
                .HasForeignKey(ph => ph.ProgressionRuleId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // 4. Fix WorkoutExercise <-> ExerciseType relationship to prevent ExerciseTypeId1 shadow property
            modelBuilder.Entity<WorkoutExercise>()
                .HasOne(we => we.ExerciseType)
                .WithMany(et => et.WorkoutExercises)
                .HasForeignKey(we => we.ExerciseTypeId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Configure Property Access Mode
            modelBuilder.Entity<WorkoutExercise>()
                .Navigation(we => we.ExerciseType)
                .UsePropertyAccessMode(PropertyAccessMode.Property);
            
            // Configure Equipment relationship separately
            modelBuilder.Entity<WorkoutExercise>()
                .HasOne(we => we.Equipment)
                .WithMany()
                .HasForeignKey(we => we.EquipmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // 5. Fix WorkoutSchedule <-> TemplateAssignment relationship to prevent TemplateAssignmentId1 shadow property
            // UPDATED: Keep only one configuration for this relationship to avoid ambiguity
            modelBuilder.Entity<WorkoutSchedule>()
                .HasOne(ws => ws.TemplateAssignment)
                .WithMany(ta => ta.WorkoutSchedules)
                .HasForeignKey(ws => ws.TemplateAssignmentId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure the other relationships in WorkoutSchedule
            modelBuilder.Entity<WorkoutSchedule>()
                .HasOne(w => w.LastGeneratedSession)
                .WithMany()
                .HasForeignKey(w => w.LastGeneratedSessionId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<WorkoutSchedule>()
                .HasOne(w => w.Template)
                .WithMany()
                .HasForeignKey(w => w.TemplateId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
                
            modelBuilder.Entity<WorkoutSchedule>()
                .HasOne(w => w.Client)
                .WithMany()
                .HasForeignKey(w => w.ClientUserId)
                .OnDelete(DeleteBehavior.Restrict);
                
            modelBuilder.Entity<WorkoutSchedule>()
                .HasOne(w => w.Coach) 
                .WithMany()
                .HasForeignKey(w => w.CoachUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure entity relationships for ClientActivity
            modelBuilder.Entity<ClientActivity>()
                .HasOne(ca => ca.Client)
                .WithMany()
                .HasForeignKey(ca => ca.ClientId)
                .OnDelete(DeleteBehavior.Restrict);
                
            modelBuilder.Entity<ClientActivity>()
                .HasOne(ca => ca.Coach)
                .WithMany()
                .HasForeignKey(ca => ca.CoachId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
                
            // *** OTHER RELATIONSHIP CONFIGURATIONS ***
            
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
                .HasOne(st => st.WorkoutSession)
                .WithMany()
                .HasForeignKey(st => st.WorkoutSessionId)
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

            // Configure WorkoutTemplate relationships and query filter
            modelBuilder.Entity<WorkoutTemplate>()
                .HasOne(wt => wt.User)
                .WithMany()
                .HasForeignKey(wt => wt.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // WorkoutTemplates are filtered by the current user
            modelBuilder.Entity<WorkoutTemplate>()
                .HasQueryFilter(wt => _currentUserId == null || wt.User.IdentityUserId == _currentUserId);
                
            // Configure WorkoutTemplateExercise relationships
            modelBuilder.Entity<WorkoutTemplateExercise>()
                .HasOne(wte => wte.WorkoutTemplate)
                .WithMany(wt => wt.TemplateExercises)
                .HasForeignKey(wte => wte.WorkoutTemplateId)
                .OnDelete(DeleteBehavior.Cascade);
                
            modelBuilder.Entity<WorkoutTemplateExercise>()
                .HasOne(wte => wte.ExerciseType)
                .WithMany(et => et.TemplateExercises)
                .HasForeignKey(wte => wte.ExerciseTypeId)
                .OnDelete(DeleteBehavior.Restrict);
                
            // WorkoutTemplateExercises are filtered by the current user (via WorkoutTemplate)
            modelBuilder.Entity<WorkoutTemplateExercise>()
                .HasQueryFilter(wte => _currentUserId == null || wte.WorkoutTemplate.User.IdentityUserId == _currentUserId);
                
            // Configure WorkoutTemplateSet relationships
            modelBuilder.Entity<WorkoutTemplateSet>()
                .HasOne(wts => wts.WorkoutTemplateExercise)
                .WithMany(wte => wte.TemplateSets)
                .HasForeignKey(wts => wts.WorkoutTemplateExerciseId)
                .OnDelete(DeleteBehavior.Cascade);
                
            modelBuilder.Entity<WorkoutTemplateSet>()
                .HasOne(wts => wts.Settype)
                .WithMany()
                .HasForeignKey(wts => wts.SettypeId)
                .OnDelete(DeleteBehavior.Restrict);
                
            // WorkoutTemplateSets are filtered by the current user (via WorkoutTemplateExercise -> WorkoutTemplate)
            modelBuilder.Entity<WorkoutTemplateSet>()
                .HasQueryFilter(wts => _currentUserId == null || wts.WorkoutTemplateExercise.WorkoutTemplate.User.IdentityUserId == _currentUserId);
            
            // Configure CoachClientRelationship <-> CoachClientPermission relationship
            modelBuilder.Entity<CoachClientRelationship>()
                .HasOne(r => r.Permissions)
                .WithOne(p => p.Relationship)
                .HasForeignKey<CoachClientPermission>(p => p.CoachClientRelationshipId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure relationships for ClientGroup
            modelBuilder.Entity<ClientGroup>()
                .HasOne(g => g.Coach)
                .WithMany()
                .HasForeignKey(g => g.CoachId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ClientGroup>()
                .HasMany(g => g.ClientRelationships)
                .WithOne(r => r.ClientGroup)
                .HasForeignKey(r => r.ClientGroupId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure relationships for ClientGroupMember
            modelBuilder.Entity<ClientGroupMember>()
                .HasOne(m => m.ClientGroup)
                .WithMany()
                .HasForeignKey(m => m.ClientGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ClientGroupMember>()
                .HasOne(m => m.Relationship)
                .WithMany()
                .HasForeignKey(m => m.CoachClientRelationshipId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure relationships for CoachNote
            modelBuilder.Entity<CoachNote>()
                .HasOne(n => n.Relationship)
                .WithMany(r => r.Notes)
                .HasForeignKey(n => n.CoachClientRelationshipId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Configure relationships for ClientGoal
            modelBuilder.Entity<ClientGoal>()
                .HasOne(g => g.Relationship)
                .WithMany()
                .HasForeignKey(g => g.CoachClientRelationshipId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Configure relationships for CoachClientMessage
            modelBuilder.Entity<CoachClientMessage>()
                .HasOne(m => m.Relationship)
                .WithMany()
                .HasForeignKey(m => m.CoachClientRelationshipId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Configure relationship for ExerciseFeedback to fix warnings
            modelBuilder.Entity<ExerciseFeedback>()
                .HasOne(ef => ef.WorkoutFeedback)
                .WithMany(wf => wf.ExerciseFeedbacks)
                .HasForeignKey(ef => ef.WorkoutFeedbackId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ExerciseFeedback>()
                .HasOne(ef => ef.WorkoutSet)
                .WithMany()
                .HasForeignKey(ef => ef.WorkoutSetId)
                .OnDelete(DeleteBehavior.Restrict);

            // *** QUERY FILTERS ***

            // Configure WorkoutSession and related entity query filters with matching conditions
            modelBuilder.Entity<WorkoutSession>()
                .HasQueryFilter(ws => _currentUserId == null || ws.User.IdentityUserId == _currentUserId);

            modelBuilder.Entity<WorkoutExercise>()
                .HasQueryFilter(we => _currentUserId == null || we.WorkoutSession.User.IdentityUserId == _currentUserId);

            modelBuilder.Entity<WorkoutSet>()
                .HasQueryFilter(ws => _currentUserId == null || ws.WorkoutExercise.WorkoutSession.User.IdentityUserId == _currentUserId);

            // Configure coaching entity filters
            modelBuilder.Entity<CoachClientRelationship>()
                .HasQueryFilter(r => _currentUserId == null || r.CoachId == _currentUserId || r.ClientId == _currentUserId);
                
            modelBuilder.Entity<CoachClientPermission>()
                .HasQueryFilter(p => _currentUserId == null || p.Relationship.CoachId == _currentUserId || p.Relationship.ClientId == _currentUserId);

            modelBuilder.Entity<ClientGroup>()
                .HasQueryFilter(g => _currentUserId == null || g.CoachId == _currentUserId);

            modelBuilder.Entity<ClientGroupMember>()
                .HasQueryFilter(m => _currentUserId == null || m.ClientGroup.CoachId == _currentUserId || m.Relationship.ClientId == _currentUserId);
            
            modelBuilder.Entity<CoachNote>()
                .HasQueryFilter(n => _currentUserId == null || n.Relationship.CoachId == _currentUserId || n.Relationship.ClientId == _currentUserId);

            modelBuilder.Entity<ClientGoal>()
                .HasQueryFilter(g => _currentUserId == null || g.Relationship.CoachId == _currentUserId || g.Relationship.ClientId == _currentUserId);

            modelBuilder.Entity<CoachClientMessage>()
                .HasQueryFilter(m => _currentUserId == null || m.Relationship.CoachId == _currentUserId || m.Relationship.ClientId == _currentUserId);
            
            // Configure WorkoutFeedback and ExerciseFeedback with matching query filters
            modelBuilder.Entity<WorkoutFeedback>()
                .HasQueryFilter(wf => _currentUserId == null || wf.Client.IdentityUserId == _currentUserId);

            modelBuilder.Entity<ExerciseFeedback>()
                .HasQueryFilter(ef => _currentUserId == null || ef.WorkoutFeedback.Client.IdentityUserId == _currentUserId);

            // Configure ProgressionRule and ProgressionHistory matching query filters
            modelBuilder.Entity<ProgressionRule>()
                .HasQueryFilter(pr => _currentUserId == null || pr.Coach.IdentityUserId == _currentUserId || 
                                (pr.Client != null && pr.Client.IdentityUserId == _currentUserId));

            modelBuilder.Entity<ProgressionHistory>()
                .HasQueryFilter(ph => _currentUserId == null || ph.ProgressionRule.Coach.IdentityUserId == _currentUserId || 
                               (ph.ProgressionRule.Client != null && ph.ProgressionRule.Client.IdentityUserId == _currentUserId));

            // Configure workout programming models query filters
            modelBuilder.Entity<TemplateAssignment>()
                .HasQueryFilter(ta => _currentUserId == null || ta.Coach.IdentityUserId == _currentUserId || 
                                ta.Client.IdentityUserId == _currentUserId);

            modelBuilder.Entity<WorkoutSchedule>()
                .HasQueryFilter(ws => _currentUserId == null || ws.Coach.IdentityUserId == _currentUserId || 
                               ws.Client.IdentityUserId == _currentUserId);

            modelBuilder.Entity<ClientExerciseExclusion>()
                .HasQueryFilter(cee => _currentUserId == null || cee.Client.IdentityUserId == _currentUserId || 
                                (cee.CreatedByCoach != null && cee.CreatedByCoach.IdentityUserId == _currentUserId));

            modelBuilder.Entity<ClientEquipment>()
                .HasQueryFilter(ce => _currentUserId == null || ce.Client.IdentityUserId == _currentUserId);

            // Query filter for Feedback table
            modelBuilder.Entity<Feedback>()
                .HasQueryFilter(f => _currentUserId == null || f.User == null || f.User.IdentityUserId == _currentUserId);

            // Query filter for GoalMilestones
            modelBuilder.Entity<GoalMilestone>()
                .HasQueryFilter(m => _currentUserId == null || m.Goal.UserId == _currentUserId || 
                              (m.Goal.IsVisibleToCoach && m.Goal.Relationship.CoachId == _currentUserId));

            // Query filters for dashboard entities
            modelBuilder.Entity<GoalFeedback>()
                .HasQueryFilter(gf => _currentUserId == null || gf.CoachId == _currentUserId || gf.Goal.UserId == _currentUserId);

            modelBuilder.Entity<ClientActivity>()
                .HasQueryFilter(ca => _currentUserId == null || ca.ClientId == _currentUserId || ca.CoachId == _currentUserId);

            // *** INDEXES ***
            // (Indexes remain unchanged - they're not part of the shadow property issue)
            // Configure indexes for better performance
            modelBuilder.Entity<AlertThreshold>()
                .HasIndex(at => at.MetricName);
                
            modelBuilder.Entity<Alert>()
                .HasIndex(a => a.TriggeredAt);
                
            modelBuilder.Entity<AlertHistory>()
                .HasIndex(ah => ah.TriggeredAt);
                
            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.UserId, n.IsRead });

            modelBuilder.Entity<WorkoutTemplate>()
                .HasIndex(wt => wt.UserId);
                
            modelBuilder.Entity<WorkoutTemplate>()
                .HasIndex(wt => wt.Category);
                
            modelBuilder.Entity<WorkoutTemplateExercise>()
                .HasIndex(wte => wte.WorkoutTemplateId);
                
            modelBuilder.Entity<WorkoutTemplateExercise>()
                .HasIndex(wte => wte.ExerciseTypeId);
                
            modelBuilder.Entity<WorkoutTemplateExercise>()
                .HasIndex(wte => wte.SequenceNum);
                
            modelBuilder.Entity<WorkoutTemplateSet>()
                .HasIndex(wts => wts.WorkoutTemplateExerciseId);
                
            modelBuilder.Entity<WorkoutTemplateSet>()
                .HasIndex(wts => wts.SequenceNum);

            modelBuilder.Entity<CoachClientRelationship>()
                .HasIndex(r => r.CoachId);
                
            modelBuilder.Entity<CoachClientRelationship>()
                .HasIndex(r => r.ClientId);
                
            modelBuilder.Entity<CoachClientRelationship>()
                .HasIndex(r => r.Status);
                
            modelBuilder.Entity<CoachClientRelationship>()
                .HasIndex(r => new { r.CoachId, r.ClientId })
                .IsUnique()
                .HasFilter("[ClientId] IS NOT NULL");
                
            modelBuilder.Entity<ClientGroup>()
                .HasIndex(g => g.CoachId);
                
            modelBuilder.Entity<CoachNote>()
                .HasIndex(n => n.CoachClientRelationshipId);
                
            modelBuilder.Entity<CoachNote>()
                .HasIndex(n => n.IsVisibleToClient);
                
            modelBuilder.Entity<ClientGoal>()
                .HasIndex(g => g.CoachClientRelationshipId);
                
            modelBuilder.Entity<ClientGoal>()
                .HasIndex(g => g.IsActive);
                
            modelBuilder.Entity<GoalMilestone>()
                .HasIndex(m => m.GoalId);
                
            modelBuilder.Entity<GoalMilestone>()
                .HasIndex(m => m.Date);
                
            modelBuilder.Entity<CoachClientMessage>()
                .HasIndex(m => m.CoachClientRelationshipId);
                
            modelBuilder.Entity<TemplateAssignment>()
                .HasIndex(ta => ta.WorkoutTemplateId);
                
            modelBuilder.Entity<TemplateAssignment>()
                .HasIndex(ta => ta.ClientUserId);
                
            modelBuilder.Entity<TemplateAssignment>()
                .HasIndex(ta => ta.CoachUserId);
                
            modelBuilder.Entity<TemplateAssignment>()
                .HasIndex(ta => ta.IsActive);
                
            modelBuilder.Entity<WorkoutSchedule>()
                .HasIndex(ws => ws.TemplateAssignmentId);
                
            modelBuilder.Entity<WorkoutSchedule>()
                .HasIndex(ws => ws.ClientUserId);
                
            modelBuilder.Entity<WorkoutSchedule>()
                .HasIndex(ws => ws.CoachUserId);
                
            modelBuilder.Entity<WorkoutSchedule>()
                .HasIndex(ws => ws.IsActive);
                
            modelBuilder.Entity<WorkoutFeedback>()
                .HasIndex(wf => wf.WorkoutSessionId);
                
            modelBuilder.Entity<WorkoutFeedback>()
                .HasIndex(wf => wf.ClientUserId);
                
            modelBuilder.Entity<WorkoutFeedback>()
                .HasIndex(wf => wf.CoachNotified);
                
            modelBuilder.Entity<WorkoutFeedback>()
                .HasIndex(wf => wf.CoachViewed);
                
            modelBuilder.Entity<ExerciseFeedback>()
                .HasIndex(ef => ef.WorkoutFeedbackId);
                
            modelBuilder.Entity<ExerciseFeedback>()
                .HasIndex(ef => ef.WorkoutSetId);
                
            modelBuilder.Entity<ProgressionRule>()
                .HasIndex(pr => pr.WorkoutTemplateExerciseId);
                
            modelBuilder.Entity<ProgressionRule>()
                .HasIndex(pr => pr.WorkoutTemplateSetId);
                
            modelBuilder.Entity<ProgressionRule>()
                .HasIndex(pr => pr.ClientUserId);
                
            modelBuilder.Entity<ProgressionRule>()
                .HasIndex(pr => pr.CoachUserId);
                
            modelBuilder.Entity<ProgressionRule>()
                .HasIndex(pr => pr.IsActive);
                
            modelBuilder.Entity<ProgressionHistory>()
                .HasIndex(ph => ph.ProgressionRuleId);
                
            modelBuilder.Entity<ProgressionHistory>()
                .HasIndex(ph => ph.WorkoutSessionId);
                
            modelBuilder.Entity<ProgressionHistory>()
                .HasIndex(ph => ph.ApplicationDate);
                
            modelBuilder.Entity<ExerciseSubstitution>()
                .HasIndex(es => es.PrimaryExerciseTypeId);
                
            modelBuilder.Entity<ExerciseSubstitution>()
                .HasIndex(es => es.SubstituteExerciseTypeId);
                
            modelBuilder.Entity<ExerciseSubstitution>()
                .HasIndex(es => es.CreatedByCoachId);
                
            modelBuilder.Entity<ExerciseSubstitution>()
                .HasIndex(es => es.IsGlobal);
                
            modelBuilder.Entity<ExerciseSubstitution>()
                .HasIndex(es => es.MovementPattern);
                
            modelBuilder.Entity<ClientExerciseExclusion>()
                .HasIndex(cee => cee.ClientUserId);
                
            modelBuilder.Entity<ClientExerciseExclusion>()
                .HasIndex(cee => cee.ExerciseTypeId);
                
            modelBuilder.Entity<ClientExerciseExclusion>()
                .HasIndex(cee => cee.IsActive);
                
            modelBuilder.Entity<ClientEquipment>()
                .HasIndex(ce => ce.ClientUserId);
                
            modelBuilder.Entity<ClientEquipment>()
                .HasIndex(ce => ce.IsAvailable);
                
            modelBuilder.Entity<WorkoutSession>()
                .HasIndex(ws => ws.UserId);
                
            modelBuilder.Entity<WorkoutSession>()
                .HasIndex(ws => ws.StartDateTime);
                
            modelBuilder.Entity<WorkoutExercise>()
                .HasIndex(we => we.WorkoutSessionId);
                
            modelBuilder.Entity<WorkoutExercise>()
                .HasIndex(we => we.ExerciseTypeId);
                
            modelBuilder.Entity<WorkoutSet>()
                .HasIndex(ws => ws.WorkoutExerciseId);
                
            modelBuilder.Entity<GoalFeedback>()
                .HasIndex(gf => gf.GoalId);
                
            modelBuilder.Entity<GoalFeedback>()
                .HasIndex(gf => gf.CoachId);
                
            modelBuilder.Entity<GoalFeedback>()
                .HasIndex(gf => gf.IsRead);
                
            modelBuilder.Entity<ClientActivity>()
                .HasIndex(ca => ca.ClientId);
                
            modelBuilder.Entity<ClientActivity>()
                .HasIndex(ca => ca.CoachId);
                
            modelBuilder.Entity<ClientActivity>()
                .HasIndex(ca => ca.ActivityDate);
                
            modelBuilder.Entity<ClientActivity>()
                .HasIndex(ca => ca.IsViewedByCoach);
        }
    }
}
