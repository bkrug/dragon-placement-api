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
            [FromQuery(Name="offset")] int offset = 0,
            [FromQuery(Name="limit")] int limit = 20,
            [FromQuery(Name="jobId")] int? jobId = null
        )
    {
        var dataAsEnumerable = jobId == null
            ? unitOfWork.DragonRepository.Get()
            : unitOfWork.GetDragonsWithoutOverlappingAssignments(jobId.Value);
        var pagedData = new PagedData<Dragon>
        {
            Offset = offset,
            Limit = limit,
            TotalRecords = dataAsEnumerable.Count(),
            Data = dataAsEnumerable.Skip(offset).Take(limit).ToList()
        };
        return TypedResults.Ok(pagedData);
    }

    public static async Task<Results<Ok<ValidatedPayload<Dragon>>, NotFound<ValidatedResponse>>>
        GetDragonAsync(
            HttpContext context,
            IAssignmentUnitOfWork unitOfWork,
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
            IAssignmentUnitOfWork unitOfWork,
            [FromBody] Dragon dragon)
    {
        var validationFailures = ValidateDragon(dragon);
        if (validationFailures != null)
            return TypedResults.BadRequest(validationFailures);

        unitOfWork.DragonRepository.Insert(dragon);
        await unitOfWork.SaveAsync().ConfigureAwait(false);
        return TypedResults.Ok(ValidatedPayload<Dragon>.FromPayload(dragon));
    }

    public static async Task<Results<Ok<ValidatedResponse>, NotFound<ValidatedResponse>, BadRequest<ValidatedForm<DragonValidationFailures>>>>
        UpdateDragonAsync(
            IAssignmentUnitOfWork unitOfWork,
            [FromRoute(Name="dragonId")] int dragonId,
            [FromBody] Dragon inputDragon)
    {
        var existing = await unitOfWork.DragonRepository.GetByID(dragonId).ConfigureAwait(false);
        if (existing == null)
        {
            return TypedResults.NotFound(ValidatedResponse.NotFound);
        }

        existing.GivenName = inputDragon.GivenName;
        existing.FamilyName = inputDragon.FamilyName;
        existing.CanBreathFire = inputDragon.CanBreathFire;
        existing.CanTakePassengers = inputDragon.CanTakePassengers;
        existing.WeightInKg = inputDragon.WeightInKg;
        existing.LengthInMeters = inputDragon.LengthInMeters;
        existing.FightingSkills = inputDragon.FightingSkills;
        var validationFailures = ValidateDragon(existing);
        if (validationFailures != null)
            return TypedResults.BadRequest(validationFailures);

        await unitOfWork.SaveAsync().ConfigureAwait(false);
        return TypedResults.Ok(ValidatedResponse.Success);
    }

    private static ValidatedForm<DragonValidationFailures>? ValidateDragon(Dragon dragon)
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

    public static async Task<Results<Ok<ValidatedResponse>, NotFound<ValidatedResponse>>>
        DeleteDragonAsync(
            IAssignmentUnitOfWork unitOfWork,
            [FromRoute(Name="dragonId")] int dragonId)
    {
        var deleteResult = unitOfWork.DragonRepository.Delete(dragonId);
        if (deleteResult == DeleteResult.NotFound)
        {
            return TypedResults.NotFound(ValidatedResponse.NotFound);
        }
        await unitOfWork.SaveAsync().ConfigureAwait(false);
        return TypedResults.Ok(ValidatedResponse.Success);
    }
}
