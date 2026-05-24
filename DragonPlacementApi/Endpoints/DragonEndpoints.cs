using DragonPlacementApi.Poco;
using DragonPlacementDataLayer.Enum;
using DragonPlacementDataLayer.Models;
using DragonPlacementDataLayer.Repositories;
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
            IAssignmentUnitOfWork unitOfWork,
            [FromQuery(Name="skillTagId")] int[] skillTagIds,
            [FromQuery(Name="offset")] int offset = 0,
            [FromQuery(Name="limit")] int limit = 20,
            [FromQuery(Name="jobId")] int? jobId = null
        )
    {
        if (skillTagIds.Length > 0 && jobId == null)
            return TypedResults.BadRequest(new ValidatedResponse
            {
                IsSuccess = false,
                IsInternalError = false,
                ValidationFailures = ["Filtering by skills is only allowed when a jobId is specified"]
            });

        var dataAsEnumerable = jobId == null
            ? unitOfWork.DragonRepository.Get()
            : unitOfWork.GetDragonsWithoutOverlappingAssignments(jobId.Value, skillTagIds);
        var pagedData = new PagedData<Dragon>
        {
            Offset = offset,
            Limit = limit,
            TotalRecords = dataAsEnumerable.Count(),
            Data = dataAsEnumerable.Skip(offset).Take(limit).ToList()
        };
        return TypedResults.Ok(pagedData);
    }

    public static async Task<Results<Ok<ValidatedPayload<Dragon>>, NotFound<ValidatedResponse>, InternalServerError<ValidatedResponse>>>
        GetDragonAsync(
            IAssignmentUnitOfWork unitOfWork,
            [FromRoute(Name="dragonId")] int dragonId,
            [FromQuery(Name="jobInclusions")] JobInclusions jobInclusions = JobInclusions.None
        )
    {
        var dragons = await unitOfWork.GetDragonWithJobAsync(dragonId, jobInclusions).ConfigureAwait(false);
        return dragons.Count switch
        {
            0 => TypedResults.NotFound(ValidatedResponse.NotFound),
            1 => TypedResults.Ok(ValidatedPayload<Dragon>.FromPayload(dragons.First())),
            _ => TypedResults.InternalServerError(ValidatedResponse.ExpectedOneFoundMultiple),
        };
    }

    public static async Task<Results<Ok<ValidatedPayload<Dragon>>, BadRequest<ValidatedForm<DragonValidationFailures>>>>
        CreateDragonAsync(
            IAssignmentUnitOfWork unitOfWork,
            [FromBody] DragonCreateEdit inputDragon)
    {
        var validationFailures = ValidateDragon(inputDragon);
        if (validationFailures != null)
            return TypedResults.BadRequest(validationFailures);

        var newDragon = new Dragon
        {
            GivenName = inputDragon.GivenName,
            FamilyName = inputDragon.FamilyName,
            CanBreathFire = inputDragon.CanBreathFire,
            CanTakePassengers = inputDragon.CanTakePassengers,
            WeightInKg = inputDragon.WeightInKg,
            LengthInMeters = inputDragon.LengthInMeters,
            FightingSkills = inputDragon.FightingSkills,
            SkillTags = unitOfWork.GetSkillTagsById(inputDragon.SkillTagIds)
        };
        unitOfWork.DragonRepository.Insert(newDragon);

        await unitOfWork.SaveAsync().ConfigureAwait(false);
        return TypedResults.Ok(ValidatedPayload<Dragon>.FromPayload(newDragon));
    }

    public static async Task<Results<Ok<ValidatedPayload<Dragon>>, NotFound<ValidatedResponse>, BadRequest<ValidatedForm<DragonValidationFailures>>, InternalServerError<ValidatedResponse>>>
        UpdateDragonAsync(
            IAssignmentUnitOfWork unitOfWork,
            [FromRoute(Name="dragonId")] int dragonId,
            [FromBody] DragonCreateEdit inputDragon)
    {
        var loadedDragons = await unitOfWork.GetDragonWithJobAsync(dragonId, JobInclusions.None).ConfigureAwait(false);
        if (loadedDragons.Count == 0)
            return TypedResults.NotFound(ValidatedResponse.NotFound);
        else if (loadedDragons.Count > 1)
            return TypedResults.InternalServerError(ValidatedResponse.ExpectedOneFoundMultiple);

        var validationFailures = ValidateDragon(inputDragon);
        if (validationFailures != null)
            return TypedResults.BadRequest(validationFailures);

        var existing = loadedDragons.Single();
        existing.GivenName = inputDragon.GivenName;
        existing.FamilyName = inputDragon.FamilyName;
        existing.CanBreathFire = inputDragon.CanBreathFire;
        existing.CanTakePassengers = inputDragon.CanTakePassengers;
        existing.WeightInKg = inputDragon.WeightInKg;
        existing.LengthInMeters = inputDragon.LengthInMeters;
        existing.FightingSkills = inputDragon.FightingSkills;
        existing.SkillTags = unitOfWork.GetSkillTagsById(inputDragon.SkillTagIds);

        await unitOfWork.SaveAsync().ConfigureAwait(false);
        return TypedResults.Ok(ValidatedPayload<Dragon>.FromPayload(existing));
    }

    private static ValidatedForm<DragonValidationFailures>? ValidateDragon(DragonCreateEdit dragon)
    {
        var failures = new DragonValidationFailures();

        if (string.IsNullOrWhiteSpace(dragon.GivenName))
            failures.GivenName = "is required";
        if (dragon.WeightInKg <= 0)
            failures.WeightInKg = "must be a positive number";
        if (dragon.LengthInMeters <= 0)
            failures.LengthInMeters = "must be a positive number";
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
            IAssignmentUnitOfWork unitOfWork,
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
