using DragonCommon.Data.Repositories;
using DragonCommon.Application.Repositories;
using DragonTimekeeping.Domain.Models;
using Microsoft.EntityFrameworkCore;

using DragonTimekeeping.Application;

namespace DragonTimekeeping.Data;

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

    public async Task<PayPeriod?> GetPayPeriodWithHoursWorkedAsync(int payPeriodId)
    {
        return await _context.PayPeriods
            .Include(p => p.HoursWorked.OrderBy(hw => hw.StartDateTime))
            .FirstOrDefaultAsync(p => p.PayPeriodId == payPeriodId);
    }
}
