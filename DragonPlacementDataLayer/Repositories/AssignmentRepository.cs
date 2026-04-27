using DragonPlacementDataLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace DragonPlacementDataLayer.Repositories;

public interface IAssignmentRepository : IGenericRepository<Assignment>
{
    IEnumerable<Assignment> GetOverlappingAssignments(int dragonId, DateTime periodStart, DateTime periodEnd);
}

public class AssignmentRepository(DragonPlacementContext context) : GenericRepository<Assignment>(context), IAssignmentRepository
{
    public IEnumerable<Assignment> GetOverlappingAssignments(int dragonId, DateTime periodStart, DateTime periodEnd)
    {
        return context.Assignments
            .Where(a => a.DragonId == dragonId)
            .Where(a => (!a.EndDate.HasValue || periodStart.Date <= a.EndDate.Value.Date) && periodEnd >= a.StartDate.Date);
    }
}