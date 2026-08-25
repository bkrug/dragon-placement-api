using DragonAssignment.Application.DragonSelect;
using DragonAssignment.Application.DragonUpsert;
using DragonPlacementApi.Poco;
using DragonAssignment.Application;
using DragonAssignment.Domain.Enum;
using DragonAssignment.Domain.Models;
using DragonCommon.Domain.Poco;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using DragonAssignment.Application.DragonDelete;

namespace DragonPlacementApi.Endpoints;

public static class DragonEndpoints
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
            CancellationToken cancellationToken,
            [FromQuery(Name="jobInclusions")] JobInclusions jobInclusions = JobInclusions.None
        )
    {
        var dragon = await unitOfWork.GetDragonWithJobAsync(dragonId, jobInclusions, cancellationToken).ConfigureAwait(false);
        return dragon == null
            ? TypedResults.NotFound(ValidatedResponse.NotFound)
            : TypedResults.Ok(ValidatedPayload<Dragon>.FromPayload(dragon));
    }

    public static async Task<Results<Ok<ValidatedPayload<Dragon>>, BadRequest<ValidatedForm<ValidationFailures>>>>
        CreateDragonAsync(
            IDragonPlacementUnitOfWork unitOfWork,
            [FromBody] DragonCreateEdit inputDragon)
    {
        var result = await DragonUpsertService.CreateDragon(unitOfWork, inputDragon).ConfigureAwait(false);
        if (result.IsFailure)
            return TypedResults.BadRequest(new ValidatedForm<ValidationFailures>
            {
                IsSuccess = false,
                IsInternalError = false,
                ValidationFailures = result.Error
            });
        return TypedResults.Ok(ValidatedPayload<Dragon>.FromPayload(result.Value));
    }

    public static async Task<Results<Ok<ValidatedPayload<Dragon>>, NotFound<ValidatedResponse>, BadRequest<ValidatedForm<ValidationFailures>>>>
        UpdateDragonAsync(
            IDragonPlacementUnitOfWork unitOfWork,
            [FromRoute(Name="dragonId")] int dragonId,
            [FromBody] DragonCreateEdit inputDragon,
            CancellationToken cancellationToken)
    {
        var result = await DragonUpsertService.UpdateDragon(unitOfWork, inputDragon, dragonId, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
            return result.Error switch
            {
                DragonNotFound => TypedResults.NotFound(ValidatedResponse.NotFound),
                DragonInvalid e => TypedResults.BadRequest(new ValidatedForm<ValidationFailures>
                {
                    IsSuccess = false,
                    IsInternalError = false,
                    ValidationFailures = e.Failures
                }),
                _ => throw new InvalidOperationException()
            };
        return TypedResults.Ok(ValidatedPayload<Dragon>.FromPayload(result.Value));
    }

    public static async Task<Results<Ok<ValidatedResponse>, NotFound<ValidatedResponse>, Conflict<ValidatedResponse>>>
        DeleteDragonAsync(
            IDragonPlacementUnitOfWork unitOfWork,
            [FromRoute(Name="dragonId")] int dragonId)
    {
        var result = await DragonDeletionService.DeleteDragon(unitOfWork, dragonId).ConfigureAwait(false);
        if (result.IsFailure)
            return result.Error switch
            {
                DragonDeleteNotFound => TypedResults.NotFound(ValidatedResponse.NotFound),
                DragonDeleteHasAssignment => TypedResults.Conflict(new ValidatedResponse { ValidationFailures = ["Dragon has an existing assignment"] }),
                _ => throw new InvalidOperationException()
            };
        return TypedResults.Ok(ValidatedResponse.Success);
    }
}
