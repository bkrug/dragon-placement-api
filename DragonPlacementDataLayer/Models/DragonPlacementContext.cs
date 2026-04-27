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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Assignment>(entity =>
        {
            entity.ToTable("Assignment");

            entity.Property(e => e.AssignmentId).ValueGeneratedNever();
            entity.Property(e => e.StartDate).HasConversion(
                modelVal => new DateTimeOffset(modelVal).ToUnixTimeSeconds(),
                dbVal => DateTimeOffset.FromUnixTimeSeconds(dbVal).DateTime
            );
            entity.Property(e => e.EndDate).HasConversion(
                modelVal => modelVal == null ? (long?)null : new DateTimeOffset(modelVal.Value).ToUnixTimeSeconds(),
                dbVal => dbVal == null ? null : DateTimeOffset.FromUnixTimeSeconds(dbVal.Value).DateTime
            );

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

            entity.Property(e => e.DragonId).ValueGeneratedNever();
            entity.Property(e => e.LengthInMeters).HasColumnType("NUMERIC");
            entity.Property(e => e.WeightInKg).HasColumnType("NUMERIC");
        });

        modelBuilder.Entity<Job>(entity =>
        {
            entity.ToTable("Job");

            entity.Property(e => e.JobId).ValueGeneratedNever();
            entity.Property(e => e.NumberOfPositions).HasDefaultValue(1);
            entity.Property(e => e.StartDate).HasConversion(
                modelVal => new DateTimeOffset(modelVal).ToUnixTimeSeconds(),
                dbVal => DateTimeOffset.FromUnixTimeSeconds(dbVal).DateTime
            );
            entity.Property(e => e.EndDate).HasConversion(
                modelVal => new DateTimeOffset(modelVal).ToUnixTimeSeconds(),
                dbVal => DateTimeOffset.FromUnixTimeSeconds(dbVal).DateTime
            );            
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}