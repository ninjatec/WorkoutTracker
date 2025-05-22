using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace WorkoutTrackerWeb.TempModels;

public partial class TempDbContext : DbContext
{
    public TempDbContext()
    {
    }

    public TempDbContext(DbContextOptions<TempDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AspNetUser> AspNetUsers { get; set; }

    public virtual DbSet<CoachClientRelationship> CoachClientRelationships { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("name=ConnectionStrings:DefaultConnection");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AspNetUser>(entity =>
        {
            entity.HasIndex(e => e.NormalizedEmail, "EmailIndex");

            entity.HasIndex(e => e.NormalizedUserName, "UserNameIndex")
                .IsUnique()
                .HasFilter("([NormalizedUserName] IS NOT NULL)");

            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.NormalizedEmail).HasMaxLength(256);
            entity.Property(e => e.NormalizedUserName).HasMaxLength(256);
            entity.Property(e => e.UserName).HasMaxLength(256);
        });

        modelBuilder.Entity<CoachClientRelationship>(entity =>
        {
            entity.HasIndex(e => e.AppUserId, "IX_CoachClientRelationships_AppUserId");

            entity.HasIndex(e => e.AppUserId1, "IX_CoachClientRelationships_AppUserId1");

            entity.HasIndex(e => e.ClientGroupId, "IX_CoachClientRelationships_ClientGroupId");

            entity.HasIndex(e => e.ClientId, "IX_CoachClientRelationships_ClientId");

            entity.HasIndex(e => e.CoachId, "IX_CoachClientRelationships_CoachId");

            entity.HasIndex(e => new { e.CoachId, e.ClientId }, "IX_CoachClientRelationships_CoachId_ClientId")
                .IsUnique()
                .HasFilter("([ClientId] IS NOT NULL)");

            entity.HasIndex(e => e.Status, "IX_CoachClientRelationships_Status");

            entity.Property(e => e.CoachId).IsRequired();
            entity.Property(e => e.InvitedEmail).HasMaxLength(256);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
