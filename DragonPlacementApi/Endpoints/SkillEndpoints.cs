using DragonPlacementApi.Poco;
using DragonAssignmentApplication;
using DragonAssignmentDomain.Models;
using Microsoft.AspNetCore.Mvc;

namespace DragonPlacementApi.Endpoints;

public class SkillEndpoints
{
    public static PagedData<SkillTag> GetSkillTagsAsync(IDragonPlacementUnitOfWork unitOfWork, [FromQuery(Name="offset")] int offset = 0, [FromQuery(Name="limit")] int limit = 20)
    {
        var skillTags = unitOfWork.SkillTagRespository.Get(st => true);
        return new PagedData<SkillTag>
        {
            Offset = offset,
            Limit = limit,
            TotalRecords = skillTags.Count(),
            Data = skillTags.Skip(offset).Take(limit).ToList()
        };
    }
}
