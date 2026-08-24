using Microsoft.EntityFrameworkCore;

namespace DragonCommon.Data;

public abstract class BaseUnitOfWork<T>(T context) : IDisposable where T : DbContext
{
    protected readonly T _context = context;

    public async Task SaveChangesAsync()
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