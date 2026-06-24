using CSharpFunctionalExtensions;
using DragonAssignmentDomain.Models;
using DragonAssignmentDomain.Poco;

namespace DragonAssignmentApplication.JobUpsert;

public static class JobUpsertService
{
    public static async Task<Result<Job, JobValidationFailures>> CreateJob(JobCreateEdit input, IDragonPlacementUnitOfWork unitOfWork)
    {
        var skillTags = unitOfWork.GetSkillTagsById(input.SkillTagIds);
        var result = JobCreateEditMapper.ToJob(input, skillTags)
            .Bind(JobValidation.Validate);

        if (result.IsSuccess)
        {
            unitOfWork.JobRepository.Insert(result.Value);
            await unitOfWork.SaveAsync().ConfigureAwait(false);
        }

        return result;
    }

    public static async Task<Result<Job, JobValidationFailures>?> UpdateJob(JobCreateEdit input, int jobId, IDragonPlacementUnitOfWork unitOfWork)
    {
        var existing = await unitOfWork.GetJobWithSkillsAsync(jobId).ConfigureAwait(false);
        if (existing == null)
            return null;

        var skillTags = unitOfWork.GetSkillTagsById(input.SkillTagIds);
        var result = JobCreateEditMapper.ApplyTo(input, existing, skillTags)
            .Bind(JobValidation.Validate);

        if (result.IsSuccess)
            await unitOfWork.SaveAsync().ConfigureAwait(false);

        return result;
    }
}
