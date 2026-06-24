using DragonTimekeepingDomain.Models;
using Microsoft.EntityFrameworkCore;

namespace DragonTimekeepingData;

/// <summary>
/// There is more than one data domain in this git-repository.
/// The Timekeeping context has to do with tracking hours,
/// but it is employe-centric (dragon-centric).
/// The end user is interested in reporting their own activity and converting their hours into a paycheck.
/// The end user doesn't care about billing the customer.
/// </summary>
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

            entity.Property(e => e.StartDate)
                .HasColumnName("StartDateUnix")
                .HasConversion(
                    d => new DateTimeOffset(d, TimeSpan.Zero).ToUnixTimeSeconds(),
                    unix => DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime
                );
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
