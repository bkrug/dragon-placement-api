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

    public virtual DbSet<Dragon> Dragons { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
// TODO: Get his from appsettings.json
        => optionsBuilder.UseSqlite("Data Source=../Database/DragonPlacement.db;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Dragon>(entity =>
        {
            entity.ToTable("Dragon");

            entity.Property(e => e.DragonId).ValueGeneratedNever();
            entity.Property(e => e.LengthInMeters).HasColumnType("NUMERIC");
            entity.Property(e => e.WeightInKg).HasColumnType("NUMERIC");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
