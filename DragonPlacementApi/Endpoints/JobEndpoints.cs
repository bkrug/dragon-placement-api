using DragonPlacementDataLayer.Models;
using DragonPlacementDataLayer.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace DragonPlacementApi.Endpoints;

public class JobEndpoints
{
    public static PagedData<Job> GetJobs(DragonPlacementContext context, [FromQuery(Name="offset")] int offset = 0, [FromQuery(Name="limit")] int limit = 20) =>
        new()
        {
            Offset = offset,
            Limit = limit,
            TotalRecords = context.Jobs.Count(),
            Data = context.Jobs.Skip(offset).Take(limit).ToList()
        };

    public static PagedData<Dragon> GetAssignedDragons(
        IAssignmentUnitOfWork unitOfWork,
        [FromRoute(Name="jobId")] int jobId,
        [FromQuery(Name="offset")] int offset = 0,
        [FromQuery(Name="limit")] int limit = 20)
    {
        var dragonEnumerable = unitOfWork.AssignmentRepository.GetAssignedDragons(jobId);
        return new()
        {
            Offset = offset,
            Limit = limit,
            TotalRecords = dragonEnumerable.Count(),
            Data = dragonEnumerable.Skip(offset).Take(limit).ToList()
        };
    }
}
