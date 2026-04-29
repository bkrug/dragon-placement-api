using DragonPlacementDataLayer.Models;
using Microsoft.EntityFrameworkCore;
using DragonPlacementDataLayer.Poco;

namespace DragonPlacementDataLayer.Repositories;

public interface IAssignmentRepository : IGenericRepository<Assignment>
{
    IEnumerable<Assignment> GetOverlappingAssignments(int dragonId, long periodStartUnix, long periodEndUnix);
    IEnumerable<Dragon> GetDragonsWithoutOverlappingAssignments(int jobId);
    IEnumerable<Dragon> GetAssignedDragons(int jobId);
    IEnumerable<JobWithCapacity> GetJobsWithCapacity();
}

public class AssignmentRepository(DragonPlacementContext context) : GenericRepository<Assignment>(context), IAssignmentRepository
{
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
}