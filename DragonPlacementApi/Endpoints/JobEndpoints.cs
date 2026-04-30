using DragonPlacementDataLayer.Models;
using DragonPlacementDataLayer.Poco;
using DragonPlacementDataLayer.Repositories;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace DragonPlacementApi.Endpoints;

public class JobEndpoints
{
    public static PagedData<JobWithCapacity> GetJobs(IAssignmentUnitOfWork unitOfWork, [FromQuery(Name="offset")] int offset = 0, [FromQuery(Name="limit")] int limit = 20) {
        var jobEnumerable = unitOfWork.AssignmentRepository.GetJobsWithCapacity();
        return new()
        {
            Offset = offset,
            Limit = limit,
            TotalRecords = jobEnumerable.Count(),
            Data = jobEnumerable.Skip(offset).Take(limit).ToList()
        };        
    }

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

    public async static Task<Results<Ok<ValidatedResponse>, NotFound<ValidatedResponse>, InternalServerError<ValidatedResponse>>> UnassignDragon(
        IAssignmentUnitOfWork unitOfWork,
        [FromRoute(Name="jobId")] int jobId,
        [FromRoute(Name="dragonId")] int dragonId)
    {
        var foundAssignments = unitOfWork.AssignmentRepository.Get(asgn => asgn.JobId == jobId && asgn.DragonId == dragonId).ToList();
        if (foundAssignments.Count == 0)
        {
            return TypedResults.NotFound(ValidatedResponse.NotFound);
        }
        else if (foundAssignments.Count == 1)
        {
            unitOfWork.AssignmentRepository.Delete(foundAssignments[0]);
            await unitOfWork.SaveAsync().ConfigureAwait(false);
            return TypedResults.Ok(ValidatedResponse.Success);
        }
        else
        {
            return TypedResults.InternalServerError(ValidatedResponse.ExpectedOneFoundMultiple);
        }
    }
}
