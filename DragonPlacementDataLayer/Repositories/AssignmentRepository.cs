using DragonPlacementDataLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace DragonPlacementDataLayer.Repositories;

public interface IAssignmentRepository : IGenericRepository<Assignment>
{
    IEnumerable<Assignment> GetOverlappingAssignments(int dragonId, DateTime periodStart, DateTime periodEnd);
    IEnumerable<Dragon> GetDragonsWithoutOverlappingAssignments(int jobId);
}

public class AssignmentRepository(DragonPlacementContext context) : GenericRepository<Assignment>(context), IAssignmentRepository
{
    public IEnumerable<Assignment> GetOverlappingAssignments(int dragonId, DateTime periodStart, DateTime periodEnd)
    {
        return _context.Assignments
            .Where(a => a.DragonId == dragonId)
            .Where(a => (!a.EndDate.HasValue || periodStart.Date <= a.EndDate.Value.Date) && periodEnd >= a.StartDate.Date);
    }

    public IEnumerable<Dragon> GetDragonsWithoutOverlappingAssignments(int jobId)
    {
        var job = _context.Jobs.Find(jobId);
        if (job == null)
            return [];
        var periodStart = job.StartDate;
        var periodEnd = job.EndDate;
        return _context.Dragons
            .Where(d => d.Assignments
                .Count(a =>
                    (!a.EndDate.HasValue || periodStart.Date <= a.EndDate.Value.Date)
                    && periodEnd >= a.StartDate.Date
                ) == 0
            );
    }    
}