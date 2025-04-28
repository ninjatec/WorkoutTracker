using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Models;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using WorkoutTrackerWeb.Models.Alerting;
using WorkoutTrackerWeb.Models.Coaching;

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

        // Workout template DbSets
        public DbSet<WorkoutTrackerWeb.Models.WorkoutTemplate> WorkoutTemplate { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.WorkoutTemplateExercise> WorkoutTemplateExercise { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.WorkoutTemplateSet> WorkoutTemplateSet { get; set; } = default!;

        // Coaching system DbSets
        public DbSet<WorkoutTrackerWeb.Models.Coaching.CoachClientRelationship> CoachClientRelationships { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.Coaching.CoachClientPermission> CoachClientPermissions { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.Coaching.ClientGroup> ClientGroups { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.Coaching.ClientGroupMember> ClientGroupMembers { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.Coaching.CoachNote> CoachNotes { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.Coaching.ClientGoal> ClientGoals { get; set; } = default!;
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
                .WithMany()
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
                
            // Add indexes for better performance
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

            // Configure coaching relationships
            
            // Configure relationship between CoachClientRelationship and CoachClientPermission
            modelBuilder.Entity<CoachClientRelationship>()
                .HasOne(r => r.Permissions)
                .WithOne(p => p.Relationship)
                .HasForeignKey<CoachClientPermission>(p => p.CoachClientRelationshipId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Configure CoachClientRelationship foreign keys to prevent cascading delete cycles
            modelBuilder.Entity<CoachClientRelationship>()
                .HasOne(r => r.Coach)
                .WithMany()
                .HasForeignKey(r => r.CoachId)
                .OnDelete(DeleteBehavior.NoAction);
                
            modelBuilder.Entity<CoachClientRelationship>()
                .HasOne(r => r.Client)
                .WithMany()
                .HasForeignKey(r => r.ClientId)
                .OnDelete(DeleteBehavior.NoAction);
                
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
                
            // Query filter for ClientGroupMember
            modelBuilder.Entity<ClientGroupMember>()
                .HasQueryFilter(m => _currentUserId == null || 
                              m.ClientGroup.CoachId == _currentUserId || 
                              m.Relationship.ClientId == _currentUserId);
                              
            // Index for ClientGroupMember
            modelBuilder.Entity<ClientGroupMember>()
                .HasIndex(m => m.ClientGroupId);
                
            modelBuilder.Entity<ClientGroupMember>()
                .HasIndex(m => m.CoachClientRelationshipId);
                
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
                
            // Configure CoachClientRelationship query filter
            modelBuilder.Entity<CoachClientRelationship>()
                .HasQueryFilter(r => _currentUserId == null || r.CoachId == _currentUserId || r.ClientId == _currentUserId);
                                    
            // Configure query filters for other coaching models
            modelBuilder.Entity<ClientGroup>()
                .HasQueryFilter(g => _currentUserId == null || g.CoachId == _currentUserId);
                
            modelBuilder.Entity<CoachNote>()
                .HasQueryFilter(n => _currentUserId == null || n.Relationship.CoachId == _currentUserId || n.Relationship.ClientId == _currentUserId);
                                   
            modelBuilder.Entity<ClientGoal>()
                .HasQueryFilter(g => _currentUserId == null || g.Relationship.CoachId == _currentUserId || g.Relationship.ClientId == _currentUserId);
                                   
            modelBuilder.Entity<CoachClientMessage>()
                .HasQueryFilter(m => _currentUserId == null || m.Relationship.CoachId == _currentUserId || m.Relationship.ClientId == _currentUserId);
                                    
            // Configure indexes for coaching models
            modelBuilder.Entity<CoachClientRelationship>()
                .HasIndex(r => r.CoachId);
                
            modelBuilder.Entity<CoachClientRelationship>()
                .HasIndex(r => r.ClientId);
                
            modelBuilder.Entity<CoachClientRelationship>()
                .HasIndex(r => r.Status);
                
            modelBuilder.Entity<CoachClientRelationship>()
                .HasIndex(r => new { r.CoachId, r.ClientId }).IsUnique();
                
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
                
            modelBuilder.Entity<CoachClientMessage>()
                .HasIndex(m => m.CoachClientRelationshipId);
                
            modelBuilder.Entity<CoachClientMessage>()
                .HasIndex(m => m.IsRead);
                
            // Configure relationships and query filters for workout programming models
            
            // Explicitly configure TemplateAssignment.ClientRelationshipId
            modelBuilder.Entity<TemplateAssignment>()
                .HasOne(ta => ta.CoachClientRelationship)
                .WithMany()
                .HasForeignKey(ta => ta.ClientRelationshipId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
                
            // TemplateAssignment relationships
            modelBuilder.Entity<TemplateAssignment>()
                .HasOne(ta => ta.WorkoutTemplate)
                .WithMany()
                .HasForeignKey(ta => ta.WorkoutTemplateId)
                .OnDelete(DeleteBehavior.Restrict);
                
            modelBuilder.Entity<TemplateAssignment>()
                .HasOne(ta => ta.Client)
                .WithMany()
                .HasForeignKey(ta => ta.ClientUserId)
                .OnDelete(DeleteBehavior.Restrict);
                
            modelBuilder.Entity<TemplateAssignment>()
                .HasOne(ta => ta.Coach)
                .WithMany()
                .HasForeignKey(ta => ta.CoachUserId)
                .OnDelete(DeleteBehavior.Restrict);
                
            // WorkoutSchedule relationships
            modelBuilder.Entity<WorkoutSchedule>()
                .HasOne(ws => ws.TemplateAssignment)
                .WithMany()
                .HasForeignKey(ws => ws.TemplateAssignmentId)
                .OnDelete(DeleteBehavior.SetNull);
                
            modelBuilder.Entity<WorkoutSchedule>()
                .HasOne(ws => ws.Client)
                .WithMany()
                .HasForeignKey(ws => ws.ClientUserId)
                .OnDelete(DeleteBehavior.Restrict);
                
            modelBuilder.Entity<WorkoutSchedule>()
                .HasOne(ws => ws.Coach)
                .WithMany()
                .HasForeignKey(ws => ws.CoachUserId)
                .OnDelete(DeleteBehavior.Restrict);
                
            // Configure query filters for workout programming models
            modelBuilder.Entity<TemplateAssignment>()
                .HasQueryFilter(ta => _currentUserId == null || 
                                  ta.Coach.IdentityUserId == _currentUserId || 
                                  ta.Client.IdentityUserId == _currentUserId);
                                     
            modelBuilder.Entity<WorkoutSchedule>()
                .HasQueryFilter(ws => _currentUserId == null || 
                                 ws.Coach.IdentityUserId == _currentUserId || 
                                 ws.Client.IdentityUserId == _currentUserId);
                
            modelBuilder.Entity<WorkoutFeedback>()
                .HasQueryFilter(wf => _currentUserId == null || 
                                 wf.Client.IdentityUserId == _currentUserId);
                
            modelBuilder.Entity<ProgressionRule>()
                .HasQueryFilter(pr => _currentUserId == null || 
                                 pr.Coach.IdentityUserId == _currentUserId || 
                                 (pr.Client != null && pr.Client.IdentityUserId == _currentUserId));
                
            modelBuilder.Entity<ClientExerciseExclusion>()
                .HasQueryFilter(cee => _currentUserId == null || 
                                  cee.Client.IdentityUserId == _currentUserId || 
                                  (cee.CreatedByCoach != null && cee.CreatedByCoach.IdentityUserId == _currentUserId));
                
            modelBuilder.Entity<ClientEquipment>()
                .HasQueryFilter(ce => _currentUserId == null || 
                                ce.Client.IdentityUserId == _currentUserId);
                                                                     
            // Add indexes for workout programming models
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
                .HasIndex(wf => wf.SessionId);
                
            modelBuilder.Entity<WorkoutFeedback>()
                .HasIndex(wf => wf.ClientUserId);
                
            modelBuilder.Entity<WorkoutFeedback>()
                .HasIndex(wf => wf.CoachNotified);
                
            modelBuilder.Entity<WorkoutFeedback>()
                .HasIndex(wf => wf.CoachViewed);
                
            modelBuilder.Entity<ExerciseFeedback>()
                .HasIndex(ef => ef.WorkoutFeedbackId);
                
            modelBuilder.Entity<ExerciseFeedback>()
                .HasIndex(ef => ef.SetId);
                
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
                .HasIndex(ph => ph.SessionId);
                
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

            // ExerciseSubstitution relationships
            modelBuilder.Entity<ExerciseSubstitution>()
                .HasOne(es => es.PrimaryExercise)
                .WithMany()
                .HasForeignKey(es => es.PrimaryExerciseTypeId)
                .OnDelete(DeleteBehavior.Restrict);
                
            modelBuilder.Entity<ExerciseSubstitution>()
                .HasOne(es => es.SubstituteExercise)
                .WithMany()
                .HasForeignKey(es => es.SubstituteExerciseTypeId)
                .OnDelete(DeleteBehavior.Restrict);
                
            modelBuilder.Entity<ExerciseSubstitution>()
                .HasOne(es => es.CreatedByCoach)
                .WithMany()
                .HasForeignKey(es => es.CreatedByCoachId)
                .OnDelete(DeleteBehavior.Restrict);

            // WorkoutFeedback relationships
            modelBuilder.Entity<WorkoutFeedback>()
                .HasOne(wf => wf.Session)
                .WithMany()
                .HasForeignKey(wf => wf.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
                
            modelBuilder.Entity<WorkoutFeedback>()
                .HasOne(wf => wf.Client)
                .WithMany()
                .HasForeignKey(wf => wf.ClientUserId)
                .OnDelete(DeleteBehavior.Restrict);
                
            // ExerciseFeedback relationships
            modelBuilder.Entity<ExerciseFeedback>()
                .HasOne(ef => ef.WorkoutFeedback)
                .WithMany(wf => wf.ExerciseFeedbacks)
                .HasForeignKey(ef => ef.WorkoutFeedbackId)
                .OnDelete(DeleteBehavior.Cascade);
                
            modelBuilder.Entity<ExerciseFeedback>()
                .HasOne(ef => ef.Set)
                .WithMany()
                .HasForeignKey(ef => ef.SetId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
