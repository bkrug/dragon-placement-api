using CSharpFunctionalExtensions;
using DragonAssignment.Domain.Models;
using DragonCommon.Domain.Poco;

namespace DragonAssignment.Application.JobUpsert;

public abstract record JobUpdateFailure;
public record JobNotFound : JobUpdateFailure;
public record JobInvalid(ValidationFailures Failures) : JobUpdateFailure;

public static class JobUpsertService
{
    public static async Task<Result<Job, ValidationFailures>> CreateJob(
        IDragonPlacementUnitOfWork unitOfWork, JobCreateEdit input)
    {
        var skillTags = unitOfWork.GetSkillTagsById(input.SkillTagIds);
        return await JobCreateEditMapper.ToJob(input, skillTags)
            .Bind(j => j.Validate())
            .Tap(job => unitOfWork.JobRepository.Insert(job))
            .Tap(async _ => await unitOfWork.SaveChangesAsync().ConfigureAwait(false));
    }

    public static async Task<Result<Job, JobUpdateFailure>> UpdateJob(IDragonPlacementUnitOfWork unitOfWork, JobCreateEdit input, int jobId)
    {
        var existing = await unitOfWork.GetJobWithSkillsAsync(jobId).ConfigureAwait(false);
        if (existing == null)
            return Result.Failure<Job, JobUpdateFailure>(new JobNotFound());

        var skillTags = unitOfWork.GetSkillTagsById(input.SkillTagIds);
        return await JobCreateEditMapper.ApplyTo(input, existing, skillTags)
            .Bind(j => j.Validate())
            .MapError(e => (JobUpdateFailure)new JobInvalid(e))
            .Tap(async _ => await unitOfWork.SaveChangesAsync().ConfigureAwait(false));
    }
}
