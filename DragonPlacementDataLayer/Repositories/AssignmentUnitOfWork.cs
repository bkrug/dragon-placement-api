using System;
using System.Linq.Expressions;
using DragonPlacementDataLayer.Enum;
using DragonPlacementDataLayer.Models;
using DragonPlacementDataLayer.Poco;
using Microsoft.EntityFrameworkCore;

namespace DragonPlacementDataLayer.Repositories;

public interface IAssignmentUnitOfWork
{
    IGenericRepository<Dragon> DragonRepository { get; }
    IGenericRepository<Job> JobRepository { get; }
    IGenericRepository<Assignment> AssignmentRepository { get; }

    void Dispose();
    Task SaveAsync();

    Task<Dragon?> GetDragonWithJobAsync(int dragonId, JobInclusions jobInclusions);
    IEnumerable<Assignment> GetOverlappingAssignments(int dragonId, long periodStartUnix, long periodEndUnix);
    IEnumerable<Dragon> GetDragonsWithoutOverlappingAssignments(int jobId);
    IEnumerable<Dragon> GetAssignedDragons(int jobId);
    IEnumerable<JobWithCapacity> GetJobsWithCapacity();    
}

public class AssignmentUnitOfWork(DragonPlacementContext context) : IDisposable, IAssignmentUnitOfWork
{
    private readonly DragonPlacementContext _context = context;
    public IGenericRepository<Dragon> DragonRepository { get; } = new GenericRepository<Dragon>(context);
    public IGenericRepository<Job> JobRepository { get; } = new GenericRepository<Job>(context);
    public IGenericRepository<Assignment> AssignmentRepository { get; } = new GenericRepository<Assignment>(context);

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

    public IEnumerable<Assignment> GetOverlappingAssignments(int dragonId, long periodStartUnix, long periodEndUnix)
    {
        return _context.Assignments
            .Where(a => a.DragonId == dragonId)
            .Where(a => (!a.EndDateUnix.HasValue || periodStartUnix <= a.EndDateUnix.Value) && periodEndUnix >= a.StartDateUnix);
    }

    public IEnumerable<Dragon> GetDragonsWithoutOverlappingAssignments(int jobId)
    {
        var job = _context.Jobs.Find(jobId);
        if (job == null)
            return [];
        var periodStart = job.StartDateUnix;
        var periodEnd = job.EndDateUnix;
        return _context.Dragons
            .Where(d => d.Assignments
                .Count(a =>
                    (!a.EndDateUnix.HasValue || periodStart <= a.EndDateUnix.Value)
                    && periodEnd >= a.StartDateUnix
                ) == 0
            );
    }

    public IEnumerable<Dragon> GetAssignedDragons(int jobId)
    {
        return _context.Assignments
            .Where(a => a.JobId == jobId)
            .Select(a => a.Dragon);
    }

    public IEnumerable<JobWithCapacity> GetJobsWithCapacity()
    {
        return _context.Jobs
            .Select(j => new JobWithCapacity
            {
                JobId = j.JobId,
                JobTitle = j.JobTitle,
                EmployerName = j.EmployerName,
                NumberOfPositions = j.NumberOfPositions,
                FilledPositions = j.Assignments.Count(),
                StartDateUnix = j.StartDateUnix,
                EndDateUnix = j.EndDateUnix                
            });
    }    

    public async Task<Dragon?> GetDragonWithJobAsync(int dragonId, JobInclusions jobInclusions)
    {
        var todayUnix = new DateTimeOffset(DateTime.UtcNow.Date).ToUnixTimeSeconds();
        IQueryable<Dragon> dragonEnumerable = jobInclusions switch
        {
            JobInclusions.Past => _context.Dragons
                .Include(d => d.Assignments.Where(a => a.EndDateUnix.HasValue && a.EndDateUnix < todayUnix))
                    .ThenInclude(a => a.Job),
            JobInclusions.CurrentAndFuture => _context.Dragons
                .Include(d => d.Assignments.Where(a => a.StartDateUnix >= todayUnix))
                    .ThenInclude(a => a.Job),
            _ => _context.Dragons
        };
        return await dragonEnumerable
            .Where(d => d.DragonId == dragonId)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);
    }
}