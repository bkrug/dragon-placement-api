using DragonCommon.Application.Repositories;
using DragonCommon.Data;
using DragonCommon.Data.Repositories;
using DragonTimekeeping.Application;
using DragonTimekeeping.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DragonTimekeeping.Data;

public class TimekeepingUnitOfWork(TimekeepingContext context) : BaseUnitOfWork<TimekeepingContext>(context), IDisposable, ITimekeepingUnitOfWork
{
    public IGenericRepository<HoursWorked> HoursWorkedRepository { get; } = new GenericRepository<HoursWorked>(context);
    public IGenericRepository<PayPeriod> PayPeriodRepository { get; } = new GenericRepository<PayPeriod>(context);

    public async Task<PayPeriod?> GetPayPeriodWithHoursWorkedAsync(int payPeriodId)
    {
        return await _context.PayPeriods
            .Include(p => p.HoursWorked.OrderBy(hw => hw.StartDateTime))
            .FirstOrDefaultAsync(p => p.PayPeriodId == payPeriodId);
    }
}
