using DragonPlacementApi.Poco;
using DragonPlacementDataLayer.Models;
using DragonPlacementDataLayer.Poco;
using DragonPlacementDataLayer.Repositories;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace DragonPlacementApi.Endpoints;

public class JobEndpoints
{
    public static PagedData<JobWithCapacity> GetJobs(IAssignmentUnitOfWork unitOfWork, [FromQuery(Name="offset")] int offset = 0, [FromQuery(Name="limit")] int limit = 20) {
        var jobEnumerable = unitOfWork.GetJobsWithCapacity();
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
        AssignDragonToJobAsync(IAssignmentUnitOfWork unitOfWork, [FromRoute(Name="dragonId")] int dragonId, [FromRoute(Name="jobId")] int jobId)

    {
        var newJob = await unitOfWork.JobRepository.GetByID(jobId).ConfigureAwait(false);
        if (newJob == null)
        {
            return TypedResults.NotFound(new ValidatedResponse
            {
               IsSuccess = false,
               IsInternalError = false,
               ValidationFailures = [ "Job does not exist" ]
            });
        }
        var existingJobs = unitOfWork.GetOverlappingAssignments(dragonId, newJob.StartDateUnix, newJob.EndDateUnix);
        var firstConflict = existingJobs.FirstOrDefault();
        if (firstConflict == null) {
            var assignmentRecord = new Assignment
            {
                DragonId = dragonId,
                JobId = jobId,
                StartDateUnix = newJob.StartDateUnix,
                EndDateUnix = newJob.EndDateUnix
            };
            unitOfWork.AssignmentRepository.Insert(assignmentRecord);
            await unitOfWork.SaveAsync().ConfigureAwait(false);
            return TypedResults.Ok(new ValidatedResponse
            {
                IsSuccess = true
            });
        }
        else {
            var periodStart = firstConflict.GetStartDate().ToShortDateString();
            var periodEnd = firstConflict.GetEndDate().ToShortDateString();
            return TypedResults.BadRequest(new ValidatedResponse
            {
                IsInternalError = false,
                IsSuccess = false,
                ValidationFailures = [ $"Overlaps with at least one job which has period of {periodStart} to {periodEnd}" ]
            });
        }
    }    

    public static async Task<Results<Ok<ValidatedPayload<Job>>, BadRequest<ValidatedForm<JobValidationFailures>>>>
        CreateJobAsync(
            IAssignmentUnitOfWork unitOfWork,
            [FromBody] Job job)
    {
        var validationFailures = ValidateJob(job);
        if (validationFailures != null)
            return TypedResults.BadRequest(validationFailures);

        unitOfWork.JobRepository.Insert(job);
        await unitOfWork.SaveAsync().ConfigureAwait(false);
        return TypedResults.Ok(ValidatedPayload<Job>.FromPayload(job));
    }

    public static async Task<Results<Ok<ValidatedPayload<Job>>, NotFound<ValidatedResponse>, BadRequest<ValidatedForm<JobValidationFailures>>>>
        UpdateJobAsync(
            IAssignmentUnitOfWork unitOfWork,
            [FromRoute(Name="jobId")] int jobId,
            [FromBody] Job inputJob)
    {
        var existing = await unitOfWork.JobRepository.GetByID(jobId).ConfigureAwait(false);
        if (existing == null)
            return TypedResults.NotFound(ValidatedResponse.NotFound);

        existing.JobTitle = inputJob.JobTitle;
        existing.EmployerName = inputJob.EmployerName;
        existing.NumberOfPositions = inputJob.NumberOfPositions;
        existing.StartDateUnix = inputJob.StartDateUnix;
        existing.EndDateUnix = inputJob.EndDateUnix;
        var validationFailures = ValidateJob(existing);
        if (validationFailures != null)
            return TypedResults.BadRequest(validationFailures);

        await unitOfWork.SaveAsync().ConfigureAwait(false);
        return TypedResults.Ok(ValidatedPayload<Job>.FromPayload(existing));
    }

    public static async Task<Results<Ok<ValidatedResponse>, NotFound<ValidatedResponse>, Conflict<ValidatedResponse>>>
        DeleteJobAsync(
            IAssignmentUnitOfWork unitOfWork,
            [FromRoute(Name="jobId")] int jobId)
    {
        if (await unitOfWork.JobHasAnAssignment(jobId).ConfigureAwait(false))
            return TypedResults.Conflict(new ValidatedResponse { ValidationFailures = ["Job has an existing assignment"] });

        var deleteResult = unitOfWork.JobRepository.Delete(jobId);
        if (deleteResult == DeleteResult.NotFound)
            return TypedResults.NotFound(ValidatedResponse.NotFound);

        await unitOfWork.SaveAsync().ConfigureAwait(false);
        return TypedResults.Ok(ValidatedResponse.Success);
    }

    private static ValidatedForm<JobValidationFailures>? ValidateJob(Job job)
    {
        var failures = new JobValidationFailures();

        if (string.IsNullOrWhiteSpace(job.JobTitle))
            failures.JobTitle = "is required";
        if (job.NumberOfPositions <= 0)
            failures.NumberOfPositions = "must be a positive number";

        if (failures.JobTitle != null || failures.NumberOfPositions != null)
            return new ValidatedForm<JobValidationFailures>
            {
                IsSuccess = false,
                IsInternalError = false,
                ValidationFailures = failures
            };

        return null;
    }

    public async static Task<Results<Ok<ValidatedResponse>, NotFound<ValidatedResponse>, InternalServerError<ValidatedResponse>>> UnassignDragonFromJobAsync(
        IAssignmentUnitOfWork unitOfWork,
        [FromRoute(Name="jobId")] int jobId,
        [FromRoute(Name="dragonId")] int dragonId)
    {
        var foundAssignments = unitOfWork.AssignmentRepository.Get(asgn => asgn.JobId == jobId && asgn.DragonId == dragonId).ToList();
        switch(foundAssignments.Count)
        {
            case 0:
                return TypedResults.NotFound(ValidatedResponse.NotFound);
            case 1:
                unitOfWork.AssignmentRepository.Delete(foundAssignments[0]);
                await unitOfWork.SaveAsync().ConfigureAwait(false);
                return TypedResults.Ok(ValidatedResponse.Success);
            default:
                return TypedResults.InternalServerError(ValidatedResponse.ExpectedOneFoundMultiple);
        }
    }
}
