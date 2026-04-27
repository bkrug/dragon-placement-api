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

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlite("Data Source=../Database/DragonPlacement.db");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Assignment>(entity =>
        {
            entity.ToTable("Assignment");

            entity.Property(e => e.AssignmentId).ValueGeneratedNever();

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
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
