using CSharpFunctionalExtensions;
using DragonAssignmentDomain.Models;
using DragonCommonDomain.Poco;

namespace DragonAssignmentApplication.DragonUpsert;

public abstract record DragonUpdateFailure;
public record DragonNotFound : DragonUpdateFailure;
public record DragonInvalid(ValidationFailures Failures) : DragonUpdateFailure;

public static class DragonUpsertService
{
    public static async Task<Result<Dragon, ValidationFailures>> CreateDragon(DragonCreateEdit input, IDragonPlacementUnitOfWork unitOfWork)
    {
        var skillTags = unitOfWork.GetSkillTagsById(input.SkillTagIds);
        var result = DragonCreateEditMapper.ToDragon(input, skillTags)
            .Bind(DragonValidation.Validate);

        if (result.IsSuccess)
        {
            unitOfWork.DragonRepository.Insert(result.Value);
            await unitOfWork.SaveAsync().ConfigureAwait(false);
        }

        return result;
    }

    public static async Task<Result<Dragon, DragonUpdateFailure>> UpdateDragon(DragonCreateEdit input, int dragonId, IDragonPlacementUnitOfWork unitOfWork)
    {
        var existing = await unitOfWork.GetDragonWithJobAsync(dragonId, DragonAssignmentDomain.Enum.JobInclusions.None).ConfigureAwait(false);
        if (existing == null)
            return Result.Failure<Dragon, DragonUpdateFailure>(new DragonNotFound());

        var skillTags = unitOfWork.GetSkillTagsById(input.SkillTagIds);
        return await DragonCreateEditMapper.ApplyTo(input, existing, skillTags)
            .Bind(DragonValidation.Validate)
            .MapError(e => (DragonUpdateFailure)new DragonInvalid(e))
            .Tap(async _ => await unitOfWork.SaveAsync().ConfigureAwait(false));
    }
}
