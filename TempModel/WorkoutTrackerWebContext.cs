using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace WorkoutTrackerWeb.TempModel;

public partial class WorkoutTrackerWebContext : DbContext
{
    public WorkoutTrackerWebContext(DbContextOptions<WorkoutTrackerWebContext> options)
        : base(options)
    {
    }

    public virtual DbSet<WorkoutFeedback> WorkoutFeedbacks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkoutFeedback>(entity =>
        {
            entity.HasKey(e => e.WorkoutFeedbackId).HasFillFactor(80);

            entity.HasIndex(e => e.ClientUserId, "IX_WorkoutFeedbacks_ClientUserId").HasFillFactor(80);

            entity.HasIndex(e => e.CoachNotified, "IX_WorkoutFeedbacks_CoachNotified").HasFillFactor(80);

            entity.HasIndex(e => e.CoachViewed, "IX_WorkoutFeedbacks_CoachViewed").HasFillFactor(80);

            entity.HasIndex(e => e.SessionId, "IX_WorkoutFeedbacks_SessionId").HasFillFactor(80);

            entity.Property(e => e.Comments).HasMaxLength(1000);
            entity.Property(e => e.IncompleteReason).HasMaxLength(1000);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
