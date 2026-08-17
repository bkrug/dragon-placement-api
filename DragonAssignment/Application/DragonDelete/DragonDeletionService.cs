using CSharpFunctionalExtensions;
using DragonCommon.Application.Repositories;

namespace DragonAssignment.Application.DragonDelete;

public abstract record DragonDeleteFailure;
public record DragonDeleteNotFound : DragonDeleteFailure;
public record DragonDeleteHasAssignment : DragonDeleteFailure;

public static class DragonDeletionService
{
    public static async Task<UnitResult<DragonDeleteFailure>> DeleteDragon(int dragonId, IDragonPlacementUnitOfWork unitOfWork)
    {
        if (await unitOfWork.DragonHasAnAssignment(dragonId).ConfigureAwait(false))
            return UnitResult.Failure<DragonDeleteFailure>(new DragonDeleteHasAssignment());

        var deleteResult = unitOfWork.DragonRepository.Delete(dragonId);
        if (deleteResult == DeleteResult.NotFound)
            return UnitResult.Failure<DragonDeleteFailure>(new DragonDeleteNotFound());

        await unitOfWork.SaveAsync().ConfigureAwait(false);
        return UnitResult.Success<DragonDeleteFailure>();
    }
}
