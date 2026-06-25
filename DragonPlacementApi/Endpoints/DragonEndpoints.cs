using DragonCommonApplication;
using DragonCommonApplication.Repositories;
using DragonAssignmentApplication.DragonSelect;
using DragonPlacementApi.Poco;
using DragonAssignmentApplication;
using DragonAssignmentDomain.Enum;
using DragonAssignmentDomain.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace DragonPlacementApi.Endpoints;

public class DragonEndpoints
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="unitOfWork"></param>
    /// <param name="offset"></param>
    /// <param name="limit"></param>
    /// <param name="jobId">If non-null, only return dragons that do not have overlapping assignments</param>
    /// <returns></returns>
    public static Results<Ok<PagedData<Dragon>>, BadRequest<ValidatedResponse>> 
        GetDragons(
            IDragonPlacementUnitOfWork unitOfWork,
            [FromQuery(Name="skillTagId")] int[] skillTagIds,
            [FromQuery(Name="offset")] int offset = 0,
            [FromQuery(Name="limit")] int limit = 20,
            [FromQuery(Name="fightingSkill")] string? fightingSkill = null,
            [FromQuery(Name="jobId")] int? jobId = null
        )
    {
        var result = DragonSelectService.GetDragons(unitOfWork, skillTagIds, fightingSkill, jobId);
        if (result.IsFailure)
            return result.Error switch
            {
                DragonSelectInvalidFilter e => TypedResults.BadRequest(new ValidatedResponse
                {
                    ValidationFailures = [e.Message]
                }),
                _ => throw new InvalidOperationException()
            };
        else {
            return TypedResults.Ok(new PagedData<Dragon>
            {
                Offset = offset,
                Limit = limit,
                TotalRecords = result.Value.Count(),
                Data = result.Value.Skip(offset).Take(limit).ToList()
            });
        }
    }

    public static async Task<Results<Ok<ValidatedPayload<Dragon>>, NotFound<ValidatedResponse>>>
        GetDragonAsync(
            IDragonPlacementUnitOfWork unitOfWork,
            [FromRoute(Name="dragonId")] int dragonId,
            [FromQuery(Name="jobInclusions")] JobInclusions jobInclusions = JobInclusions.None
        )
    {
        var dragon = await unitOfWork.GetDragonWithJobAsync(dragonId, jobInclusions).ConfigureAwait(false);
        return dragon == null
            ? TypedResults.NotFound(ValidatedResponse.NotFound)
            : TypedResults.Ok(ValidatedPayload<Dragon>.FromPayload(dragon));
    }

    public static async Task<Results<Ok<ValidatedPayload<Dragon>>, BadRequest<ValidatedForm<DragonValidationFailures>>>>
        CreateDragonAsync(
            IDragonPlacementUnitOfWork unitOfWork,
            [FromBody] DragonCreateEdit inputDragon)
    {
        var newDragon = new Dragon
        {
            GivenName = inputDragon.GivenName,
            FamilyName = inputDragon.FamilyName,
            WeightInKg = inputDragon.WeightInKg,
            LengthInMeters = inputDragon.LengthInMeters,
            FightingSkills = inputDragon.FightingSkills,
            SkillTags = unitOfWork.GetSkillTagsById(inputDragon.SkillTagIds)
        };
        var validationFailures = ValidateDragon(newDragon);
        if (validationFailures != null)
            return TypedResults.BadRequest(validationFailures);

        unitOfWork.DragonRepository.Insert(newDragon);

        await unitOfWork.SaveAsync().ConfigureAwait(false);
        return TypedResults.Ok(ValidatedPayload<Dragon>.FromPayload(newDragon));
    }

    public static async Task<Results<Ok<ValidatedPayload<Dragon>>, NotFound<ValidatedResponse>, BadRequest<ValidatedForm<DragonValidationFailures>>>>
        UpdateDragonAsync(
            IDragonPlacementUnitOfWork unitOfWork,
            [FromRoute(Name="dragonId")] int dragonId,
            [FromBody] DragonCreateEdit inputDragon)
    {
        var existing = await unitOfWork.GetDragonWithJobAsync(dragonId, JobInclusions.None).ConfigureAwait(false);
        if (existing == null)
            return TypedResults.NotFound(ValidatedResponse.NotFound);

        existing.GivenName = inputDragon.GivenName;
        existing.FamilyName = inputDragon.FamilyName;
        existing.WeightInKg = inputDragon.WeightInKg;
        existing.LengthInMeters = inputDragon.LengthInMeters;
        existing.FightingSkills = inputDragon.FightingSkills;
        existing.SkillTags = unitOfWork.GetSkillTagsById(inputDragon.SkillTagIds);

        var validationFailures = ValidateDragon(existing);
        if (validationFailures != null)
            return TypedResults.BadRequest(validationFailures);

        await unitOfWork.SaveAsync().ConfigureAwait(false);
        return TypedResults.Ok(ValidatedPayload<Dragon>.FromPayload(existing));
    }

    private static ValidatedForm<DragonValidationFailures>? ValidateDragon(Dragon dragon)
    {
        var failures = new DragonValidationFailures();

        if (string.IsNullOrWhiteSpace(dragon.GivenName))
            failures.GivenName = ValidationMessages.IS_REQUIRED;
        if (dragon.WeightInKg <= 0)
            failures.WeightInKg = ValidationMessages.MUST_BE_A_POSITIVE_NUMBER;
        if (dragon.LengthInMeters <= 0)
            failures.LengthInMeters = ValidationMessages.MUST_BE_A_POSITIVE_NUMBER;
        if (dragon.FightingSkills != null && dragon.FightingSkills is not ("b" or "m" or "a"))
            failures.FightingSkills = "must be 'b', 'm', or 'a'";

        if (failures.GivenName != null
            || failures.WeightInKg != null || failures.LengthInMeters != null || failures.FightingSkills != null)
        {
            return new ValidatedForm<DragonValidationFailures>
            {
                IsSuccess = false,
                IsInternalError = false,
                ValidationFailures = failures
            }; 
        }

        return null;
    }

    public static async Task<Results<Ok<ValidatedResponse>, NotFound<ValidatedResponse>, Conflict<ValidatedResponse>>>
        DeleteDragonAsync(
            IDragonPlacementUnitOfWork unitOfWork,
            [FromRoute(Name="dragonId")] int dragonId)
    {
        if (await unitOfWork.DragonHasAnAssignment(dragonId).ConfigureAwait(false))
        {
            return TypedResults.Conflict(new ValidatedResponse { ValidationFailures = ["Dragon has an existing assignment"] });
        }
        var deleteResult = unitOfWork.DragonRepository.Delete(dragonId);
        if (deleteResult == DeleteResult.NotFound)
        {
            return TypedResults.NotFound(ValidatedResponse.NotFound);
        }
        await unitOfWork.SaveAsync().ConfigureAwait(false);
        return TypedResults.Ok(ValidatedResponse.Success);
    }
}
