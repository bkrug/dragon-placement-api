using DragonCommonApplication.Repositories;
using DragonTimekeeping.Domain.Models;

namespace DragonTimekeeping.Application;

public interface ITimekeepingUnitOfWork
{
    IGenericRepository<HoursWorked> HoursWorkedRepository { get; }
    IGenericRepository<PayPeriod> PayPeriodRepository { get; }

    void Dispose();
    Task SaveAsync();

    Task<PayPeriod?> GetPayPeriodWithHoursWorkedAsync(int payPeriodId);
}
