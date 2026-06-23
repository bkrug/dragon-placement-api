using CommonDataLayer.Repositories;
using DragonPlacementApi.Poco;
using DragonPlacementDataLayer;
using DragonAssignmentDomain.Enum;
using DragonAssignmentDomain.Models;
using DragonAssignmentDomain.Poco;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace DragonPlacementApi.Endpoints;

public class JobEndpoints
{
    public static async Task<Results<Ok<ValidatedPayload<Job>>, NotFound<ValidatedResponse>, InternalServerError<ValidatedResponse>>> 
        GetJob(
            IDragonPlacementUnitOfWork unitOfWork,
            [FromRoute(Name="jobId")] int jobId = 0
        )
    {
        var jobs = await unitOfWork.GetJobWithSkillsAsync(jobId).ConfigureAwait(false);
        return jobs.Count switch
        {
            0 => TypedResults.NotFound(ValidatedResponse.NotFound),
            1 => TypedResults.Ok(ValidatedPayload<Job>.FromPayload(jobs.First())),
            _ => TypedResults.InternalServerError(ValidatedResponse.ExpectedOneFoundMultiple),
        };
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
            IDragonPlacementUnitOfWork unitOfWork,
            [FromBody] JobCreateEdit inputJob)
    {
        var validationFailures = ValidateJob(inputJob);
        if (validationFailures != null)
            return TypedResults.BadRequest(validationFailures);

        var job = new Job
        {
            JobTitle = inputJob.JobTitle,
            EmployerName = inputJob.EmployerName,
            NumberOfPositions = inputJob.NumberOfPositions,
            StartDateUnix = inputJob.StartDateUnix,
            EndDateUnix = inputJob.EndDateUnix,
            SkillTags = unitOfWork.GetSkillTagsById(inputJob.SkillTagIds)
        };
        unitOfWork.JobRepository.Insert(job);
        await unitOfWork.SaveAsync().ConfigureAwait(false);
        return TypedResults.Ok(ValidatedPayload<Job>.FromPayload(job));
    }

    public static async Task<Results<Ok<ValidatedPayload<Job>>, NotFound<ValidatedResponse>, BadRequest<ValidatedForm<JobValidationFailures>>, InternalServerError<ValidatedResponse>>>
        UpdateJobAsync(
            IDragonPlacementUnitOfWork unitOfWork,
            [FromRoute(Name="jobId")] int jobId,
            [FromBody] JobCreateEdit inputJob)
    {
        var loadedJobs = await unitOfWork.GetJobWithSkillsAsync(jobId).ConfigureAwait(false);
        if (loadedJobs.Count == 0)
            return TypedResults.NotFound(ValidatedResponse.NotFound);
        else if (loadedJobs.Count > 1)
            return TypedResults.InternalServerError(ValidatedResponse.ExpectedOneFoundMultiple);

        var validationFailures = ValidateJob(inputJob);
        if (validationFailures != null)
            return TypedResults.BadRequest(validationFailures);

        var existing = loadedJobs.Single();
        existing.JobTitle = inputJob.JobTitle;
        existing.EmployerName = inputJob.EmployerName;
        existing.NumberOfPositions = inputJob.NumberOfPositions;
        existing.StartDateUnix = inputJob.StartDateUnix;
        existing.EndDateUnix = inputJob.EndDateUnix;
        existing.SkillTags = unitOfWork.GetSkillTagsById(inputJob.SkillTagIds);

        await unitOfWork.SaveAsync().ConfigureAwait(false);
        return TypedResults.Ok(ValidatedPayload<Job>.FromPayload(existing));
    }

    public static async Task<Results<Ok<ValidatedResponse>, NotFound<ValidatedResponse>, Conflict<ValidatedResponse>>>
        DeleteJobAsync(
            IDragonPlacementUnitOfWork unitOfWork,
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

    private static ValidatedForm<JobValidationFailures>? ValidateJob(JobCreateEdit job)
    {
        var failures = new JobValidationFailures();

        if (string.IsNullOrWhiteSpace(job.JobTitle))
            failures.JobTitle = "is required";
        if (job.NumberOfPositions <= 0)
            failures.NumberOfPositions = "must be a positive number";
        if (job.StartDateUnix % Const.SECONDS_IN_A_DAY != 0)
            failures.StartDateUnix = "must be midnight UTC";
        if (job.EndDateUnix % Const.SECONDS_IN_A_DAY != 0)
            failures.EndDateUnix = "must be midnight UTC";

        if (failures.JobTitle != null || failures.NumberOfPositions != null
            || failures.StartDateUnix != null || failures.EndDateUnix != null)
            return new ValidatedForm<JobValidationFailures>
            {
                IsSuccess = false,
                IsInternalError = false,
                ValidationFailures = failures
            };

        return null;
    }

    public async static Task<Results<Ok<ValidatedResponse>, NotFound<ValidatedResponse>, InternalServerError<ValidatedResponse>>> UnassignDragonFromJobAsync(
        IDragonPlacementUnitOfWork unitOfWork,
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
