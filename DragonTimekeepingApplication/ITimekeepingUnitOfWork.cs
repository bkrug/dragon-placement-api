using CommonDataLayer.Repositories;
using DragonTimekeepingDomain.Models;

namespace DragonTimekeepingApplication;

public interface ITimekeepingUnitOfWork
{
    void Dispose();
    Task SaveAsync();

    IEnumerable<PayPeriod> GetPayPeriodsByAssignment(int assignmentId);
    Task<PayPeriod?> GetPayPeriodWithHoursWorkedAsync(int payPeriodId);
    void InsertPayPeriod(PayPeriod payPeriod);
    DeleteResult DeletePayPeriod(int payPeriodId);
}
