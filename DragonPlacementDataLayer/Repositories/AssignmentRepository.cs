using DragonPlacementDataLayer.Models;

namespace DragonPlacementDataLayer.Repositories;

public interface IAssignmentRepository : IGenericRepository<Assignment>
{
    IEnumerable<Assignment> GetDragonAssignments(int dragonId);
}

public class AssignmentRepository(DragonPlacementContext context) : GenericRepository<Assignment>(context), IAssignmentRepository
{
    public IEnumerable<Assignment> GetDragonAssignments(int dragonId)
    {
        return context.Assignments
            .Where(a => a.DragonId == dragonId);
    }
}