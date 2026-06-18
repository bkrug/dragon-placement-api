using DragonPlacementDataLayer.Models;
using DragonPlacementDataLayer.Poco;
using Microsoft.EntityFrameworkCore;

namespace DragonPlacementDataLayer.Repositories;

public interface ITimekeepingUnitOfWork
{
    IGenericRepository<HoursWorked> HoursWorkedRepository { get; }
    IGenericRepository<PayPeriod> PayPeriodRepository { get; }

    void Dispose();
    Task SaveAsync();

    IEnumerable<HoursWorkedWithJob> GetHoursWorkedWithJob(int dragonId, int? assignmentId);
}

public class TimekeepingUnitOfWork(TimekeepingContext context) : IDisposable, ITimekeepingUnitOfWork
{
    private readonly TimekeepingContext _context = context;
    public IGenericRepository<HoursWorked> HoursWorkedRepository { get; } = new GenericRepository<HoursWorked>(context);
    public IGenericRepository<PayPeriod> PayPeriodRepository { get; } = new GenericRepository<PayPeriod>(context);

    public async Task SaveAsync()
    {
        await _context.SaveChangesAsync();
    }

    private bool disposed = false;

    protected virtual void Dispose(bool disposing)
    {
        if (!this.disposed)
        {
            if (disposing)
            {
                _context.Dispose();
            }
        }
        this.disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public IEnumerable<HoursWorkedWithJob> GetHoursWorkedWithJob(int dragonId, int? assignmentId)
    {
        return _context.HoursWorked
            .Where(hw => hw.DragonId == dragonId && (assignmentId == null || hw.AssignmentId == assignmentId))
            .Select(hw => new HoursWorkedWithJob
            {
                HoursWorkedId = hw.HoursWorkedId,
                AssignmentId = hw.AssignmentId,
                DragonId = hw.DragonId,
                StartDateTimeUnix = hw.StartDateTimeUnix,
                EndDateTimeUnix = hw.EndDateTimeUnix,
                JobTitle = "Placeholder",
                EmployerName = "Placeholder"
            });
    }
}
