using CSharpFunctionalExtensions;
using DragonAssignment.Domain.Models;
using DragonCommon.Domain.Poco;
using DragonAssignment.Domain.Enum;

namespace DragonAssignment.Application.DragonUpsert;

public abstract record DragonUpdateFailure;
public record DragonNotFound : DragonUpdateFailure;
public record DragonInvalid(ValidationFailures Failures) : DragonUpdateFailure;

public static class DragonUpsertService
{
    public static async Task<Result<Dragon, ValidationFailures>> CreateDragon(IDragonPlacementUnitOfWork unitOfWork, DragonCreateEdit input)
    {
        var skillTags = unitOfWork.GetSkillTagsById(input.SkillTagIds);
        var result = DragonCreateEditMapper.ToDragon(input, skillTags)
            .Bind(d => d.Validate());

        if (result.IsSuccess)
        {
            unitOfWork.DragonRepository.Insert(result.Value);
            await unitOfWork.SaveChangesAsync().ConfigureAwait(false);
        }

        return result;
    }

    public static async Task<Result<Dragon, DragonUpdateFailure>> UpdateDragon(IDragonPlacementUnitOfWork unitOfWork, DragonCreateEdit input, int dragonId, CancellationToken cancellationToken)
    {
        var existing = await unitOfWork.GetDragonWithJobAsync(dragonId, JobInclusions.None, cancellationToken).ConfigureAwait(false);
        if (existing == null)
            return Result.Failure<Dragon, DragonUpdateFailure>(new DragonNotFound());

        var skillTags = unitOfWork.GetSkillTagsById(input.SkillTagIds);
        return await DragonCreateEditMapper.ApplyTo(input, existing, skillTags)
            .Bind(d => d.Validate())
            .MapError(e => (DragonUpdateFailure)new DragonInvalid(e))
            .Tap(async _ => await unitOfWork.SaveChangesAsync().ConfigureAwait(false));
    }
}
