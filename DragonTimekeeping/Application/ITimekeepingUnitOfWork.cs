using DragonCommon.Application.Repositories;
using DragonTimekeeping.Domain.Models;

namespace DragonTimekeeping.Application;

public interface ITimekeepingUnitOfWork
{
    IGenericRepository<HoursWorked> HoursWorkedRepository { get; }
    IGenericRepository<PayPeriod> PayPeriodRepository { get; }

    void Dispose();
    Task SaveChangesAsync();

    Task<PayPeriod?> GetPayPeriodWithHoursWorkedAsync(int payPeriodId);
}
