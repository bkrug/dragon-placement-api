using DragonBilling.Application;
using DragonBilling.Domain.Models;
using DragonCommon.Application.Repositories;
using DragonCommon.Data;
using DragonCommon.Data.Repositories;

namespace DragonBilling.Data;

public class BillingUnitOfWork(BillingContext context) : BaseUnitOfWork<BillingContext>(context), IBillingUnitOfWork
{
    public IGenericRepository<Customer> CustomerRepository { get; } = new GenericRepository<Customer>(context);
    public IGenericRepository<WorkRequest> WorkRequestRepository { get; } = new GenericRepository<WorkRequest>(context);
    public IGenericRepository<ChargeRate> ChargeRateRepository { get; } = new GenericRepository<ChargeRate>(context);
    public IGenericRepository<BillableHours> BillableHoursRepository { get; } = new GenericRepository<BillableHours>(context);
}
