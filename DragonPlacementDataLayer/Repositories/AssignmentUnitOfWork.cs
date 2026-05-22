using System;
using System.Linq.Expressions;
using DragonPlacementDataLayer.Enum;
using DragonPlacementDataLayer.Models;
using DragonPlacementDataLayer.Poco;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DragonPlacementDataLayer.Repositories;

public interface IAssignmentUnitOfWork
{
    IGenericRepository<Dragon> DragonRepository { get; }
    IGenericRepository<Job> JobRepository { get; }
    IGenericRepository<Assignment> AssignmentRepository { get; }
    IGenericRepository<SkillTag> SkillTagRespository { get; }

    void Dispose();
    Task SaveAsync();

    // Many results
    IEnumerable<Assignment> GetOverlappingAssignments(int dragonId, long periodStartUnix, long periodEndUnix);
    IEnumerable<Dragon> GetDragonsWithoutOverlappingAssignments(int jobId);
    IEnumerable<Dragon> GetAssignedDragons(int jobId);
    IEnumerable<JobWithCapacity> GetJobsWithCapacity();
    // 0, 1, or 2 results
    Task<IList<Dragon>> GetDragonWithJobAsync(int dragonId, JobInclusions jobInclusions);
    Task<IList<Job>> GetJobWithSkillsAsync(int jobId);
    //
    Task<bool> DragonHasAnAssignment(int dragonId);
    Task<bool> JobHasAnAssignment(int jobId);
}

public class AssignmentUnitOfWork(DragonPlacementContext context, ILogger<AssignmentUnitOfWork> logger) : IDisposable, IAssignmentUnitOfWork
{
    private readonly DragonPlacementContext _context = context;
    public IGenericRepository<Dragon> DragonRepository { get; } = new GenericRepository<Dragon>(context);
    public IGenericRepository<Job> JobRepository { get; } = new GenericRepository<Job>(context);
    public IGenericRepository<Assignment> AssignmentRepository { get; } = new GenericRepository<Assignment>(context);
    public IGenericRepository<SkillTag> SkillTagRespository { get; } = new GenericRepository<SkillTag>(context);
    private readonly ILogger<AssignmentUnitOfWork> _logger = logger;

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
            .Where(a => periodStartUnix <= a.EndDateUnix && periodEndUnix >= a.StartDateUnix);
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
                    periodStart <= a.EndDateUnix
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

    public async Task<IList<Dragon>> GetDragonWithJobAsync(int dragonId, JobInclusions jobInclusions)
    {
        var todayUnix = new DateTimeOffset(DateTime.UtcNow.Date).ToUnixTimeSeconds();
        IQueryable<Dragon> dragonEnumerable = jobInclusions switch
        {
            JobInclusions.Past => _context.Dragons
                .Include(d => d.Assignments.Where(a => a.EndDateUnix < todayUnix))
                    .ThenInclude(a => a.Job),
            JobInclusions.CurrentAndFuture => _context.Dragons
                .Include(d => d.Assignments.Where(a => a.StartDateUnix >= todayUnix))
                    .ThenInclude(a => a.Job),
            _ => _context.Dragons
        };
        return await dragonEnumerable
            .Include(d => d.SkillTags)
            .Where(d => d.DragonId == dragonId)
            .Take(2)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task<IList<Job>> GetJobWithSkillsAsync(int jobId)
    {
        return await _context.Jobs
            .Include(d => d.SkillTags)
            .Where(d => d.JobId == jobId)
            .Take(2)
            .ToListAsync()
            .ConfigureAwait(false);
    }    

    public async Task<bool> DragonHasAnAssignment(int dragonId)
    {
        return await _context.Assignments.AnyAsync(a => a.DragonId == dragonId);
    }

    public async Task<bool> JobHasAnAssignment(int jobId)
    {
        return await _context.Assignments.AnyAsync(a => a.JobId == jobId);
    }
}