using CSharpFunctionalExtensions;
using DragonAssignmentDomain.Models;
using DragonAssignmentDomain.Poco;

namespace DragonAssignmentApplication.JobUpsert;

public static class JobCreateEditMapper
{
    public static Result<Job, JobValidationFailures> ToJob(JobCreateEdit input, IList<SkillTag> skillTags)
    {
        var job = new Job
        {
            JobTitle = input.JobTitle,
            EmployerName = input.EmployerName,
            NumberOfPositions = input.NumberOfPositions,
            StartDate = DateTimeOffset.FromUnixTimeSeconds(input.StartDateUnix).UtcDateTime,
            EndDate = DateTimeOffset.FromUnixTimeSeconds(input.EndDateUnix).UtcDateTime,
            SkillTags = skillTags
        };
        return Result.Success<Job, JobValidationFailures>(job);
    }

    public static Result<Job, JobValidationFailures> ApplyTo(JobCreateEdit input, Job existing, IList<SkillTag> skillTags)
    {
        existing.JobTitle = input.JobTitle;
        existing.EmployerName = input.EmployerName;
        existing.NumberOfPositions = input.NumberOfPositions;
        existing.StartDate = DateTimeOffset.FromUnixTimeSeconds(input.StartDateUnix).UtcDateTime;
        existing.EndDate = DateTimeOffset.FromUnixTimeSeconds(input.EndDateUnix).UtcDateTime;
        existing.SkillTags = skillTags;
        return Result.Success<Job, JobValidationFailures>(existing);
    }
}
