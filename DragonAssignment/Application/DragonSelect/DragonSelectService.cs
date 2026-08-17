using CSharpFunctionalExtensions;
using DragonAssignment.Domain.Models;

namespace DragonAssignment.Application.DragonSelect;

public abstract record DragonSelectFailure;
public record DragonSelectInvalidFilter(string Message) : DragonSelectFailure;

public static class DragonSelectService
{
    public static Result<IEnumerable<Dragon>, DragonSelectFailure> GetDragons(
        IDragonPlacementUnitOfWork unitOfWork,
        int[] skillTagIds,
        string? fightingSkill,
        int? jobId)
    {
        if (skillTagIds.Length > 0 && jobId == null)
            return Result.Failure<IEnumerable<Dragon>, DragonSelectFailure>(
                new DragonSelectInvalidFilter("Filtering by skills is only allowed when a jobId is specified"));

        var dragons = jobId == null
            ? unitOfWork.DragonRepository.Get()
            : unitOfWork.GetDragonsWithoutOverlappingAssignments(jobId.Value, skillTagIds, fightingSkill);

        return Result.Success<IEnumerable<Dragon>, DragonSelectFailure>(dragons);
    }
}
