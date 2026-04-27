using DragonPlacementDataLayer.Models;
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
}
