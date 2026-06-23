using CommonDataLayer.Repositories;
using DragonAssignmentDomain.Enum;
using DragonAssignmentDomain.Poco;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DragonAssignmentApplication;
using EfModels = DragonAssignmentData.Models;
using DomainModels = DragonAssignmentDomain.Models;

namespace DragonAssignmentData;

public class DragonPlacementUnitOfWork(DragonPlacementContext context, ILogger<DragonPlacementUnitOfWork> logger) : IDisposable, IDragonPlacementUnitOfWork
{
    private readonly DragonPlacementContext _context = context;
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

    // Dragon CRUD

    public IEnumerable<DomainModels.Dragon> GetDragons()
    {
        return _context.Dragons.AsEnumerable().Select(MapDragon);
    }

    public void InsertDragon(DomainModels.Dragon dragon)
    {
        var ef = MapToEfDragon(dragon);
        _context.Dragons.Add(ef);
    }

    public DeleteResult DeleteDragon(int dragonId)
    {
        var entity = _context.Dragons.Find(dragonId);
        if (entity == null)
            return DeleteResult.NotFound;
        _context.Dragons.Remove(entity);
        return DeleteResult.Deleted;
    }

    // Job CRUD

    public async Task<DomainModels.Job?> GetJobByIdAsync(int jobId)
    {
        var ef = await _context.Jobs.FindAsync(jobId);
        return ef == null ? null : MapJob(ef);
    }

    public void InsertJob(DomainModels.Job job)
    {
        var ef = MapToEfJob(job);
        _context.Jobs.Add(ef);
    }

    public DeleteResult DeleteJob(int jobId)
    {
        var entity = _context.Jobs.Find(jobId);
        if (entity == null)
            return DeleteResult.NotFound;
        _context.Jobs.Remove(entity);
        return DeleteResult.Deleted;
    }

    // Assignment CRUD

    public void InsertAssignment(DomainModels.Assignment assignment)
    {
        var ef = MapToEfAssignment(assignment);
        _context.Assignments.Add(ef);
    }

    public IEnumerable<DomainModels.Assignment> GetAssignmentsByJobAndDragon(int jobId, int dragonId)
    {
        return _context.Assignments
            .Where(a => a.JobId == jobId && a.DragonId == dragonId)
            .AsEnumerable()
            .Select(MapAssignment);
    }

    public void DeleteAssignment(DomainModels.Assignment assignment)
    {
        var ef = _context.Assignments.Find(assignment.AssignmentId);
        if (ef != null)
            _context.Assignments.Remove(ef);
    }

    // SkillTag queries

    public IEnumerable<DomainModels.SkillTag> GetSkillTags()
    {
        return _context.SkillTags.OrderBy(st => st.SkillName).AsEnumerable().Select(MapSkillTag);
    }

    public IList<DomainModels.SkillTag> GetSkillTagsById(IList<int> skillTagIds)
    {
        return _context.SkillTags
            .Where(st => skillTagIds.Contains(st.SkillTagId))
            .AsEnumerable()
            .Select(MapSkillTag)
            .ToList();
    }

    // Complex queries

    public IEnumerable<DomainModels.Assignment> GetOverlappingAssignments(int dragonId, DateTime periodStart, DateTime periodEnd)
    {
        var periodStartUnix = new DateTimeOffset(periodStart, TimeSpan.Zero).ToUnixTimeSeconds();
        var periodEndUnix = new DateTimeOffset(periodEnd, TimeSpan.Zero).ToUnixTimeSeconds();
        return _context.Assignments
            .Where(a => a.DragonId == dragonId)
            .Where(a => periodStartUnix <= a.EndDateUnix && periodEndUnix >= a.StartDateUnix)
            .AsEnumerable()
            .Select(MapAssignment);
    }

    public IEnumerable<DomainModels.Dragon> GetDragonsWithoutOverlappingAssignments(int jobId, int[] skillTagIds, string? fightingSkill)
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
        return queryable.AsEnumerable().Select(MapDragon);
    }

    public IEnumerable<DomainModels.Dragon> GetAssignedDragons(int jobId)
    {
        return _context.Dragons
            .Include(d => d.SkillTags)
            .Where(d => d.Assignments.Any(a => a.JobId == jobId))
            .AsEnumerable()
            .Select(MapDragon);
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
            .Select(j => new
            {
                j.JobId,
                j.JobTitle,
                j.EmployerName,
                j.NumberOfPositions,
                FilledPositions = j.Assignments.Count(),
                j.StartDateUnix,
                j.EndDateUnix
            })
            .AsEnumerable()
            .Select(j => new JobWithCapacity
            {
                JobId = j.JobId,
                JobTitle = j.JobTitle,
                EmployerName = j.EmployerName,
                NumberOfPositions = j.NumberOfPositions,
                FilledPositions = j.FilledPositions,
                StartDate = DateTimeOffset.FromUnixTimeSeconds(j.StartDateUnix).UtcDateTime,
                EndDate = DateTimeOffset.FromUnixTimeSeconds(j.EndDateUnix).UtcDateTime
            });
    }

    public async Task<IList<DomainModels.Dragon>> GetDragonWithJobAsync(int dragonId, JobInclusions jobInclusions)
    {
        var todayUnix = new DateTimeOffset(DateTime.UtcNow.Date).ToUnixTimeSeconds();
        IQueryable<EfModels.Dragon> dragonEnumerable = jobInclusions switch
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
        var efList = await dragonEnumerable
            .Include(d => d.SkillTags)
            .Where(d => d.DragonId == dragonId)
            .Take(2)
            .ToListAsync()
            .ConfigureAwait(false);
        return efList.Select(MapDragon).ToList();
    }

    public async Task<IList<DomainModels.Job>> GetJobWithSkillsAsync(int jobId)
    {
        var efList = await _context.Jobs
            .Include(d => d.SkillTags)
            .Where(d => d.JobId == jobId)
            .Take(2)
            .ToListAsync()
            .ConfigureAwait(false);
        return efList.Select(MapJob).ToList();
    }

    public async Task<DomainModels.Assignment?> GetAssignmentWithDragonAndJobAsync(int assignmentId)
    {
        var ef = await _context.Assignments
            .Include(a => a.Dragon)
            .Include(a => a.Job)
            .FirstOrDefaultAsync(a => a.AssignmentId == assignmentId);
        return ef == null ? null : MapAssignment(ef);
    }

    public async Task<bool> DragonHasAnAssignment(int dragonId)
    {
        return await _context.Assignments.AnyAsync(a => a.DragonId == dragonId);
    }

    public async Task<bool> JobHasAnAssignment(int jobId)
    {
        return await _context.Assignments.AnyAsync(a => a.JobId == jobId);
    }

    // Mapping: EF → Domain

    private static DomainModels.Dragon MapDragon(EfModels.Dragon ef)
    {
        return new DomainModels.Dragon
        {
            DragonId = ef.DragonId,
            GivenName = ef.GivenName,
            FamilyName = ef.FamilyName,
            WeightInKg = ef.WeightInKg,
            LengthInMeters = ef.LengthInMeters,
            FightingSkills = ef.FightingSkills,
            Assignments = ef.Assignments.Select(MapAssignment).ToList(),
            SkillTags = ef.SkillTags.Select(MapSkillTag).ToList()
        };
    }

    private static DomainModels.Job MapJob(EfModels.Job ef)
    {
        return new DomainModels.Job
        {
            JobId = ef.JobId,
            JobTitle = ef.JobTitle,
            EmployerName = ef.EmployerName,
            NumberOfPositions = ef.NumberOfPositions,
            StartDate = DateTimeOffset.FromUnixTimeSeconds(ef.StartDateUnix).UtcDateTime,
            EndDate = DateTimeOffset.FromUnixTimeSeconds(ef.EndDateUnix).UtcDateTime,
            Assignments = ef.Assignments.Select(MapAssignment).ToList(),
            SkillTags = ef.SkillTags.Select(MapSkillTag).ToList()
        };
    }

    private static DomainModels.Assignment MapAssignment(EfModels.Assignment ef)
    {
        return new DomainModels.Assignment
        {
            AssignmentId = ef.AssignmentId,
            DragonId = ef.DragonId,
            JobId = ef.JobId,
            StartDate = DateTimeOffset.FromUnixTimeSeconds(ef.StartDateUnix).UtcDateTime,
            EndDate = DateTimeOffset.FromUnixTimeSeconds(ef.EndDateUnix).UtcDateTime,
            Dragon = ef.Dragon != null ? MapDragon(ef.Dragon) : null!,
            Job = ef.Job != null ? MapJob(ef.Job) : null!
        };
    }

    private static DomainModels.SkillTag MapSkillTag(EfModels.SkillTag ef)
    {
        return new DomainModels.SkillTag
        {
            SkillTagId = ef.SkillTagId,
            SkillName = ef.SkillName
        };
    }

    // Mapping: Domain → EF

    private static EfModels.Dragon MapToEfDragon(DomainModels.Dragon domain)
    {
        return new EfModels.Dragon
        {
            DragonId = domain.DragonId,
            GivenName = domain.GivenName,
            FamilyName = domain.FamilyName,
            WeightInKg = domain.WeightInKg,
            LengthInMeters = domain.LengthInMeters,
            FightingSkills = domain.FightingSkills
        };
    }

    private static EfModels.Job MapToEfJob(DomainModels.Job domain)
    {
        return new EfModels.Job
        {
            JobId = domain.JobId,
            JobTitle = domain.JobTitle,
            EmployerName = domain.EmployerName,
            NumberOfPositions = domain.NumberOfPositions,
            StartDateUnix = new DateTimeOffset(domain.StartDate, TimeSpan.Zero).ToUnixTimeSeconds(),
            EndDateUnix = new DateTimeOffset(domain.EndDate, TimeSpan.Zero).ToUnixTimeSeconds()
        };
    }

    private static EfModels.Assignment MapToEfAssignment(DomainModels.Assignment domain)
    {
        return new EfModels.Assignment
        {
            AssignmentId = domain.AssignmentId,
            DragonId = domain.DragonId,
            JobId = domain.JobId,
            StartDateUnix = new DateTimeOffset(domain.StartDate, TimeSpan.Zero).ToUnixTimeSeconds(),
            EndDateUnix = new DateTimeOffset(domain.EndDate, TimeSpan.Zero).ToUnixTimeSeconds()
        };
    }
}
