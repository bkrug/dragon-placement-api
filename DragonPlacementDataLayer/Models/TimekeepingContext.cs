using Microsoft.EntityFrameworkCore;

namespace DragonPlacementDataLayer.Models;

public partial class TimekeepingContext : DbContext
{
    public TimekeepingContext()
    {
    }

    public TimekeepingContext(DbContextOptions<TimekeepingContext> options)
        : base(options)
    {
    }

    public virtual DbSet<HoursWorked> HoursWorked { get; set; }

    public virtual DbSet<PayPeriod> PayPeriods { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HoursWorked>(entity =>
        {
            entity.ToTable("HoursWorked");

            entity.HasKey(e => e.HoursWorkedId);

            entity.HasOne(e => e.PayPeriod)
                .WithMany(p => p.HoursWorked)
                .HasForeignKey(e => e.PayPeriodId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PayPeriod>(entity =>
        {
            entity.ToTable("PayPeriod");

            entity.HasKey(e => e.PayPeriodId);

            entity.Ignore(e => e.Assignment);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
