using System;
using System.Linq.Expressions;
using DragonPlacementDataLayer.Enum;
using DragonPlacementDataLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace DragonPlacementDataLayer.Repositories;

public interface IAssignmentUnitOfWork
{
    IGenericRepository<Dragon> DragonRepository { get; }
    IGenericRepository<Job> JobRepository { get; }
    IAssignmentRepository AssignmentRepository { get; }

    void Dispose();
    Task SaveAsync();

    Task<Dragon?> GetDragonWithJobAsync(int dragonId, JobInclusions jobInclusions);
}

public class AssignmentUnitOfWork(DragonPlacementContext context) : IDisposable, IAssignmentUnitOfWork
{
    private readonly DragonPlacementContext _context = context;
    public IGenericRepository<Dragon> DragonRepository { get; } = new GenericRepository<Dragon>(context);
    public IGenericRepository<Job> JobRepository { get; } = new GenericRepository<Job>(context);
    public IAssignmentRepository AssignmentRepository { get; } = new AssignmentRepository(context);

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

    public async Task<Dragon?> GetDragonWithJobAsync(int dragonId, JobInclusions jobInclusions)
    {
        var todayUnix = new DateTimeOffset(DateTime.UtcNow.Date).ToUnixTimeSeconds();
        IEnumerable<Dragon> dragonEnumerable = jobInclusions switch
        {
            JobInclusions.Past => _context.Dragons
                .Include(d => d.Assignments.Where(a => a.StartDateUnix >= todayUnix))
                    .ThenInclude(a => a.Job),
            JobInclusions.CurrentAndFuture => _context.Dragons
                .Include(d => d.Assignments.Where(a => a.StartDateUnix >= todayUnix))
                    .ThenInclude(a => a.Job),
            _ => _context.Dragons
        };
        Expression<Func<Dragon, bool>> b = d => d.DragonId == dragonId;
        return await _context.Dragons
            .Include(d => d.Assignments.Where(a => a.StartDateUnix >= todayUnix))
                .ThenInclude(a => a.Job)
            .Where(b)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);
    }
}