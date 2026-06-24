using DragonCommonApplication.Repositories;
using DragonTimekeepingDomain.Models;

namespace DragonTimekeepingApplication;

public interface ITimekeepingUnitOfWork
{
    IGenericRepository<HoursWorked> HoursWorkedRepository { get; }
    IGenericRepository<PayPeriod> PayPeriodRepository { get; }

    void Dispose();
    Task SaveAsync();

    Task<PayPeriod?> GetPayPeriodWithHoursWorkedAsync(int payPeriodId);
}
