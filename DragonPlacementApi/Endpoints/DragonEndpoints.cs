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

    public static async Task<Results<Ok<ValidatedPayload<Dragon>>, BadRequest<ValidatedResponse>>>
        CreateDragonAsync(
            IAssignmentUnitOfWork unitOfWork,
            [FromBody] Dragon dragon)
    {
        if (string.IsNullOrWhiteSpace(dragon.GivenName))
        {
            return TypedResults.BadRequest(new ValidatedResponse
            {
                IsSuccess = false,
                IsInternalError = false,
                ValidationFailures = ["GivenName is required"]
            });
        }
        unitOfWork.DragonRepository.Insert(dragon);
        await unitOfWork.SaveAsync().ConfigureAwait(false);
        return TypedResults.Ok(ValidatedPayload<Dragon>.FromPayload(dragon));
    }

    public static async Task<Results<Ok<ValidatedResponse>, NotFound<ValidatedResponse>>>
        UpdateDragonAsync(
            IAssignmentUnitOfWork unitOfWork,
            [FromRoute(Name="dragonId")] int dragonId,
            [FromBody] Dragon dragon)
    {
        var existing = await unitOfWork.DragonRepository.GetByID(dragonId).ConfigureAwait(false);
        if (existing == null)
        {
            return TypedResults.NotFound(ValidatedResponse.NotFound);
        }
        existing.GivenName = dragon.GivenName;
        existing.FamilyName = dragon.FamilyName;
        existing.CanBreathFire = dragon.CanBreathFire;
        existing.CanTakePassengers = dragon.CanTakePassengers;
        existing.WeightInKg = dragon.WeightInKg;
        existing.LengthInMeters = dragon.LengthInMeters;
        existing.FightingSkills = dragon.FightingSkills;
        await unitOfWork.SaveAsync().ConfigureAwait(false);
        return TypedResults.Ok(ValidatedResponse.Success);
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
