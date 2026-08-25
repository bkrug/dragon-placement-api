using DragonCommon.Application.Repositories;
using DragonBilling.Domain.Models;

namespace DragonBilling.Application;

public interface IBillingUnitOfWork
{
    IGenericRepository<Customer> CustomerRepository { get; }
    IGenericRepository<WorkRequest> WorkRequestRepository { get; }
    IGenericRepository<ChargeRate> ChargeRateRepository { get; }
    IGenericRepository<BillableHours> BillableHoursRepository { get; }

    void Dispose();
    Task SaveChangesAsync();
}
