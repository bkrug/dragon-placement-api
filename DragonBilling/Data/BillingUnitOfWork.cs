using DragonBilling.Application;
using DragonBilling.Domain.Models;
using DragonCommon.Application.Repositories;
using DragonCommon.Data;
using DragonCommon.Data.Repositories;

namespace DragonBilling.Data;

public class BillingUnitOfWork(BillingContext context) : BaseUnitOfWork<BillingContext>(context), IDisposable, IBillingUnitOfWork
{
    public IGenericRepository<ChargeRate> ChargeRateRepository { get; } = new GenericRepository<ChargeRate>(context);
    public IGenericRepository<BillableHours> BillableHoursRepository { get; } = new GenericRepository<BillableHours>(context);
}
