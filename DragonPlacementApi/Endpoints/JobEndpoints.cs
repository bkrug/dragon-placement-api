using DragonPlacementApi.Poco;
using DragonAssignment.Application;
using DragonAssignment.Application.DragonAssignment;
using DragonAssignment.Application.JobDelete;
using DragonAssignment.Application.JobUpsert;
using DragonAssignment.Domain.Enum;
using DragonAssignment.Domain.Models;
using DragonAssignment.Domain.Poco;
using DragonCommon.Domain.Poco;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace DragonPlacementApi.Endpoints;

public static class JobEndpoints
{
    public static async Task<Results<Ok<ValidatedPayload<Job>>, NotFound<ValidatedResponse>>>
        GetJob(
            IDragonPlacementUnitOfWork unitOfWork,
            [FromRoute(Name="jobId")] int jobId = 0
        )
    {
        var job = await unitOfWork.GetJobWithSkillsAsync(jobId).ConfigureAwait(false);
        if (job == null)
            return TypedResults.NotFound(ValidatedResponse.NotFound);
        return TypedResults.Ok(ValidatedPayload<Job>.FromPayload(job));
    }

    public static PagedData<JobWithCapacity>
        GetJobs(
            IDragonPlacementUnitOfWork unitOfWork,
            [FromQuery(Name="offset")] int offset = 0,
            [FromQuery(Name="limit")] int limit = 20,
            [FromQuery(Name="jobInclusions")] JobInclusions jobInclusions = JobInclusions.All
        )
    {
        var jobEnumerable = unitOfWork.GetJobsWithCapacity(jobInclusions);
        return new()
        {
            Offset = offset,
            Limit = limit,
            TotalRecords = jobEnumerable.Count(),
            Data = jobEnumerable.Skip(offset).Take(limit).ToList()
        };        
    }

    public static PagedData<Dragon> GetAssignedDragons(
        IDragonPlacementUnitOfWork unitOfWork,
        [FromRoute(Name="jobId")] int jobId,
        [FromQuery(Name="offset")] int offset = 0,
        [FromQuery(Name="limit")] int limit = 20)
    {
        var dragonEnumerable = unitOfWork.GetAssignedDragons(jobId);
        return new()
        {
            Offset = offset,
            Limit = limit,
            TotalRecords = dragonEnumerable.Count(),
            Data = dragonEnumerable.Skip(offset).Take(limit).ToList()
        };
    }

    public static async Task<Results<Ok<ValidatedResponse>, BadRequest<ValidatedResponse>, NotFound<ValidatedResponse>>>
        AssignDragonToJobAsync(IDragonPlacementUnitOfWork unitOfWork, [FromRoute(Name="dragonId")] int dragonId, [FromRoute(Name="jobId")] int jobId)
    {
        var result = await DragonAssignmentService.AssignDragonToJob(dragonId, jobId, unitOfWork).ConfigureAwait(false);
        if (result.IsFailure)
            return result.Error switch
            {
                AssignmentJobNotFound => TypedResults.NotFound(new ValidatedResponse
                {
                    ValidationFailures = ["Job does not exist"]
                }),
                AssignmentOverlap e => TypedResults.BadRequest(new ValidatedResponse
                {
                    ValidationFailures = [$"Overlaps with at least one job which has period of {e.StartDate.ToShortDateString()} to {e.EndDate.ToShortDateString()}"]
                }),
                _ => throw new InvalidOperationException()
            };
        return TypedResults.Ok(ValidatedResponse.Success);
    }    

    public static async Task<Results<Ok<ValidatedPayload<Job>>, BadRequest<ValidatedForm<ValidationFailures>>>>
        CreateJobAsync(
            IDragonPlacementUnitOfWork unitOfWork,
            [FromBody] JobCreateEdit inputJob)
    {
        var result = await JobUpsertService.CreateJob(inputJob, unitOfWork).ConfigureAwait(false);
        if (result.IsFailure)
            return TypedResults.BadRequest(new ValidatedForm<ValidationFailures>
            {
                IsSuccess = false,
                IsInternalError = false,
                ValidationFailures = result.Error
            });
        return TypedResults.Ok(ValidatedPayload<Job>.FromPayload(result.Value));
    }

    public static async Task<Results<Ok<ValidatedPayload<Job>>, NotFound<ValidatedResponse>, BadRequest<ValidatedForm<ValidationFailures>>>>
        UpdateJobAsync(
            IDragonPlacementUnitOfWork unitOfWork,
            [FromRoute(Name="jobId")] int jobId,
            [FromBody] JobCreateEdit inputJob)
    {
        var result = await JobUpsertService.UpdateJob(inputJob, jobId, unitOfWork).ConfigureAwait(false);
        if (result.IsFailure)
            return result.Error switch
            {
                JobNotFound => TypedResults.NotFound(ValidatedResponse.NotFound),
                JobInvalid e => TypedResults.BadRequest(new ValidatedForm<ValidationFailures>
                {
                    IsSuccess = false,
                    IsInternalError = false,
                    ValidationFailures = e.Failures
                }),
                _ => throw new InvalidOperationException()
            };
        return TypedResults.Ok(ValidatedPayload<Job>.FromPayload(result.Value));
    }

    public static async Task<Results<Ok<ValidatedResponse>, NotFound<ValidatedResponse>, Conflict<ValidatedResponse>>>
        DeleteJobAsync(
            IDragonPlacementUnitOfWork unitOfWork,
            [FromRoute(Name="jobId")] int jobId)
    {
        var result = await JobDeleteService.DeleteJob(jobId, unitOfWork).ConfigureAwait(false);
        if (result.IsFailure)
            return result.Error switch
            {
                JobDeleteNotFound => TypedResults.NotFound(ValidatedResponse.NotFound),
                JobDeleteHasAssignment => TypedResults.Conflict(new ValidatedResponse { ValidationFailures = ["Job has an existing assignment"] }),
                _ => throw new InvalidOperationException()
            };
        return TypedResults.Ok(ValidatedResponse.Success);
    }

    public static async Task<Results<Ok<ValidatedResponse>, NotFound<ValidatedResponse>>>
        UnassignDragonFromJobAsync(
            IDragonPlacementUnitOfWork unitOfWork,
            [FromRoute(Name="jobId")] int jobId,
            [FromRoute(Name="dragonId")] int dragonId)
    {
        var result = await DragonAssignmentService.UnassignDragonFromJob(dragonId, jobId, unitOfWork).ConfigureAwait(false);
        if (result.IsFailure)
            return TypedResults.NotFound(ValidatedResponse.NotFound);
        return TypedResults.Ok(ValidatedResponse.Success);
    }
}
