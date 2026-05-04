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
}
