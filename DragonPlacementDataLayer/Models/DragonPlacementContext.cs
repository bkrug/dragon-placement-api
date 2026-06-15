using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace DragonPlacementDataLayer.Models;

public partial class DragonPlacementContext : DbContext
{
    public DragonPlacementContext()
    {
    }

    public DragonPlacementContext(DbContextOptions<DragonPlacementContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Assignment> Assignments { get; set; }

    public virtual DbSet<Dragon> Dragons { get; set; }

    public virtual DbSet<Job> Jobs { get; set; }

    public virtual DbSet<HoursWorked> HoursWorked { get; set; }

    public virtual DbSet<SkillTag> SkillTags { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Assignment>(entity =>
        {
            entity.ToTable("Assignment");

            entity.HasIndex(e => new { e.JobId, e.DragonId }, "Assignment_UK_JobId_DragonId").IsUnique();

            entity.HasKey(e => e.AssignmentId);
            //entity.Property(e => e.AssignmentId).ValueGeneratedNever();

            entity.HasOne(d => d.Dragon).WithMany(p => p.Assignments)
                .HasForeignKey(d => d.DragonId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Job).WithMany(p => p.Assignments)
                .HasForeignKey(d => d.JobId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Dragon>(entity =>
        {
            entity.ToTable("Dragon");

            entity.HasKey(e => e.DragonId);
            //entity.Property(e => e.DragonId).ValueGeneratedNever();
            entity.Property(e => e.LengthInMeters).HasColumnType("NUMERIC");
            entity.Property(e => e.WeightInKg).HasColumnType("NUMERIC");

            entity.HasMany(d => d.SkillTags).WithMany(p => p.Dragons)
                .UsingEntity<Dictionary<string, object>>(
                    "DragonSkillTag",
                    r => r.HasOne<SkillTag>().WithMany()
                        .HasForeignKey("SkillTagId")
                        .OnDelete(DeleteBehavior.ClientSetNull),
                    l => l.HasOne<Dragon>().WithMany()
                        .HasForeignKey("DragonId")
                        .OnDelete(DeleteBehavior.ClientSetNull),
                    j =>
                    {
                        j.HasKey("DragonId", "SkillTagId");
                        j.ToTable("DragonSkillTag");
                    });
        });

        modelBuilder.Entity<Job>(entity =>
        {
            entity.ToTable("Job");

            entity.HasKey(e => e.JobId);
            //entity.Property(e => e.JobId).ValueGeneratedNever();
            entity.Property(e => e.NumberOfPositions).HasDefaultValue(1);

            entity.HasMany(d => d.SkillTags).WithMany(p => p.Jobs)
                .UsingEntity<Dictionary<string, object>>(
                    "JobSkillTag",
                    r => r.HasOne<SkillTag>().WithMany()
                        .HasForeignKey("SkillTagId")
                        .OnDelete(DeleteBehavior.ClientSetNull),
                    l => l.HasOne<Job>().WithMany()
                        .HasForeignKey("JobId")
                        .OnDelete(DeleteBehavior.ClientSetNull),
                    j =>
                    {
                        j.HasKey("JobId", "SkillTagId");
                        j.ToTable("JobSkillTag");
                    });
        });

        modelBuilder.Entity<HoursWorked>(entity =>
        {
            entity.ToTable("HoursWorked");

            entity.HasKey(e => e.HoursWorkedId);

            entity.HasOne<Assignment>()
                .WithMany()
                .HasForeignKey(e => e.AssignmentId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne<Dragon>()
                .WithMany()
                .HasForeignKey(e => e.DragonId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<SkillTag>(entity =>
        {
            entity.ToTable("SkillTag");

            entity.HasKey(e => e.SkillTagId);
            //entity.Property(e => e.SkillTagId).ValueGeneratedNever();
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
