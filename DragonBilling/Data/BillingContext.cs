using DragonCommon.Data;
using DragonBilling.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DragonBilling.Data;

/// <summary>
/// There is more than one data domain in this git-repository.
/// The Billing context has to do with charging the customer for a dragon's work,
/// derived from the hours the dragon logged, but converted into what the customer owes.
/// </summary>
public partial class BillingContext : DbContext
{
    public BillingContext()
    {
    }

    public BillingContext(DbContextOptions<BillingContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ChargeRate> ChargeRates { get; set; }

    public virtual DbSet<BillableHours> BillableHours { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChargeRate>(entity =>
        {
            entity.ToTable("ChargeRate");

            entity.HasKey(e => e.ChargeRateId);

            entity.Property(e => e.HourlyRate).HasColumnType("NUMERIC");
        });

        modelBuilder.Entity<BillableHours>(entity =>
        {
            entity.ToTable("BillableHours");

            entity.HasKey(e => e.BillableHoursId);

            entity.Property(e => e.HourlyRate).HasColumnType("NUMERIC");
            entity.Property(e => e.TotalHours).HasColumnType("NUMERIC");
            entity.Property(e => e.Status).IsEnumNameType("Status");

            entity.HasOne(e => e.ChargeRate)
                .WithMany()
                .HasForeignKey(e => e.ChargeRateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
