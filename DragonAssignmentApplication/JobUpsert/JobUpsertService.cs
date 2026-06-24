using CSharpFunctionalExtensions;
using DragonAssignmentDomain.Models;
using DragonAssignmentDomain.Poco;

namespace DragonAssignmentApplication.JobUpsert;

public static class JobUpsertService
{
    public static Result<Job, JobValidationFailures> CreateJob(JobCreateEdit input, IDragonPlacementUnitOfWork unitOfWork)
    {
        var skillTags = unitOfWork.GetSkillTagsById(input.SkillTagIds);
        return JobCreateEditMapper.ToJob(input, skillTags)
            .Bind(JobValidation.Validate);
    }

    public static Result<Job, JobValidationFailures> UpdateJob(JobCreateEdit input, Job existing, IDragonPlacementUnitOfWork unitOfWork)
    {
        var skillTags = unitOfWork.GetSkillTagsById(input.SkillTagIds);
        return JobCreateEditMapper.ApplyTo(input, existing, skillTags)
            .Bind(JobValidation.Validate);
    }
}
