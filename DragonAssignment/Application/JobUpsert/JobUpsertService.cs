using CSharpFunctionalExtensions;
using DragonAssignment.Domain.Models;
using DragonCommon.Domain.Poco;

namespace DragonAssignment.Application.JobUpsert;

public abstract record JobUpdateFailure;
public record JobNotFound : JobUpdateFailure;
public record JobInvalid(ValidationFailures Failures) : JobUpdateFailure;

public static class JobUpsertService
{
    public static async Task<Result<Job, ValidationFailures>> CreateJob(JobCreateEdit input, IDragonPlacementUnitOfWork unitOfWork)
    {
        var skillTags = unitOfWork.GetSkillTagsById(input.SkillTagIds);
        var result = JobCreateEditMapper.ToJob(input, skillTags)
            .Bind(j => j.Validate());

        if (result.IsSuccess)
        {
            unitOfWork.JobRepository.Insert(result.Value);
            await unitOfWork.SaveAsync().ConfigureAwait(false);
        }

        return result;
    }

    public static async Task<Result<Job, JobUpdateFailure>> UpdateJob(JobCreateEdit input, int jobId, IDragonPlacementUnitOfWork unitOfWork)
    {
        var existing = await unitOfWork.GetJobWithSkillsAsync(jobId).ConfigureAwait(false);
        if (existing == null)
            return Result.Failure<Job, JobUpdateFailure>(new JobNotFound());

        var skillTags = unitOfWork.GetSkillTagsById(input.SkillTagIds);
        return await JobCreateEditMapper.ApplyTo(input, existing, skillTags)
            .Bind(j => j.Validate())
            .MapError(e => (JobUpdateFailure)new JobInvalid(e))
            .Tap(async _ => await unitOfWork.SaveAsync().ConfigureAwait(false));
    }
}
