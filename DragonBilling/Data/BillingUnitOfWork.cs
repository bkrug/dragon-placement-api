using DragonCommon.Data.Repositories;
using DragonCommon.Application.Repositories;
using DragonBilling.Domain.Models;

namespace DragonBilling.Data;

//TODO: Reduce the duplicate logic for SaveAsync() and the Dispose() methods in the various UnitOfWork classes.

public class BillingUnitOfWork(BillingContext context) : IDisposable
{
    private readonly BillingContext _context = context;
    public IGenericRepository<ChargeRate> ChargeRateRepository { get; } = new GenericRepository<ChargeRate>(context);
    public IGenericRepository<BillableHours> BillableHoursRepository { get; } = new GenericRepository<BillableHours>(context);

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
}
