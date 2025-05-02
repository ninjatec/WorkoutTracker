using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace WorkoutTrackerWeb.TempModels;

public partial class TempContext : DbContext
{
    public TempContext()
    {
    }

    public TempContext(DbContextOptions<TempContext> options)
        : base(options)
    {
    }

    public virtual DbSet<WorkoutSet> WorkoutSets { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=192.168.0.172;Database=WorkoutTrackerWeb;TrustServerCertificate=True;integrated security=False;User ID=marc.coxall;Password=Donald640060!");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkoutSet>(entity =>
        {
            entity.HasIndex(e => e.SettypeId, "IX_WorkoutSets_SettypeId");

            entity.HasIndex(e => e.WorkoutExerciseId, "IX_WorkoutSets_WorkoutExerciseId");

            entity.Property(e => e.Distance).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Notes).HasMaxLength(100);
            entity.Property(e => e.Rpe).HasColumnName("RPE");
            entity.Property(e => e.Weight).HasColumnType("decimal(10, 2)");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
