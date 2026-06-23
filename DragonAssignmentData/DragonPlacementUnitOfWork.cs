using CommonDataLayer.Repositories;
using DragonAssignmentDomain.Enum;
using DragonAssignmentDomain.Models;
using DragonAssignmentDomain.Poco;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using DragonAssignmentApplication;

namespace DragonAssignmentData;

public class DragonPlacementUnitOfWork(DragonPlacementContext context, ILogger<DragonPlacementUnitOfWork> logger) : IDisposable, IDragonPlacementUnitOfWork
{
    private readonly DragonPlacementContext _context = context;
    public IGenericRepository<Dragon> DragonRepository { get; } = new GenericRepository<Dragon>(context);
    public IGenericRepository<Job> JobRepository { get; } = new GenericRepository<Job>(context);
    public IGenericRepository<Assignment> AssignmentRepository { get; } = new GenericRepository<Assignment>(context);
    public IGenericRepository<SkillTag> SkillTagRespository { get; } = new GenericRepository<SkillTag>(context);
    private readonly ILogger<DragonPlacementUnitOfWork> _logger = logger;

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

    public IEnumerable<Dragon> GetDragonsWithoutOverlappingAssignments(int jobId, int[] skillTagIds, string? fightingSkill)
    {
        var job = _context.Jobs.Find(jobId);
        if (job == null)
            return [];
        var periodStart = job.StartDateUnix;
        var periodEnd = job.EndDateUnix;
        var queryable = _context.Dragons
            .Where(d => d.Assignments.Count(a => periodStart <= a.EndDateUnix && periodEnd >= a.StartDateUnix) == 0);
        if (skillTagIds.Length == 0)
            queryable = queryable.Where(d => skillTagIds.All(stid => d.SkillTags.Any(st => st.SkillTagId == stid)));
        if (!string.IsNullOrWhiteSpace(fightingSkill))
            queryable = queryable.Where(d => d.FightingSkills == fightingSkill);
        return queryable;
    }

    public IEnumerable<Dragon> GetAssignedDragons(int jobId)
    {
        return _context.Dragons
            .Include(d => d.SkillTags)
            .Where(d => d.Assignments.Any(a => a.JobId == jobId));
    }

    public IEnumerable<JobWithCapacity> GetJobsWithCapacity(JobInclusions jobInclusions)
    {
        var todayUnix = new DateTimeOffset(DateTime.UtcNow.Date).ToUnixTimeSeconds();
        var queryable = jobInclusions switch
        {
            JobInclusions.Past => _context.Jobs.Where(j => j.EndDateUnix < todayUnix),
            JobInclusions.CurrentAndFuture => _context.Jobs.Where(j => j.EndDateUnix >= todayUnix),
            JobInclusions.All => _context.Jobs.AsQueryable(),
            _ => _context.Jobs.Where(j => false)
        };
        return queryable
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

    public IList<SkillTag> GetSkillTagsById(IList<int> skillTagIds)
    {
        return _context.SkillTags.Where(st => skillTagIds.Contains(st.SkillTagId)).ToList();
    }

    public async Task<IList<Dragon>> GetDragonWithJobAsync(int dragonId, JobInclusions jobInclusions)
    {
        var todayUnix = new DateTimeOffset(DateTime.UtcNow.Date).ToUnixTimeSeconds();
        IQueryable<Dragon> dragonEnumerable = jobInclusions switch
        {
            JobInclusions.All => _context.Dragons
                .Include(d => d.Assignments)
                    .ThenInclude(a => a.Job),
            JobInclusions.Past => _context.Dragons
                .Include(d => d.Assignments.Where(a => a.EndDateUnix < todayUnix))
                    .ThenInclude(a => a.Job),
            JobInclusions.CurrentAndFuture => _context.Dragons
                .Include(d => d.Assignments.Where(a => a.EndDateUnix >= todayUnix))
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

    public async Task<Assignment?> GetAssignmentWithDragonAndJobAsync(int assignmentId)
    {
        return await _context.Assignments
            .Include(a => a.Dragon)
            .Include(a => a.Job)
            .FirstOrDefaultAsync(a => a.AssignmentId == assignmentId);
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