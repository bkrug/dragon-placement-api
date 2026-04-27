using DragonPlacementDataLayer.Models;
using Microsoft.AspNetCore.Mvc;

namespace DragonPlacementApi.Endpoints;

public class DragonEndpoints
{
    public static PagedData<Dragon> GetDragons(DragonPlacementContext context, [FromQuery(Name="offset")] int offset = 0, [FromQuery(Name="limit")] int limit = 20) =>
        new()
        {
            Offset = offset,
            Limit = limit,
            TotalRecords = context.Dragons.Count(),
            Data = context.Dragons.Skip(offset).Take(limit).ToList()
        };
}
