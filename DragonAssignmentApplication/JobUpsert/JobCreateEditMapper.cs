using CSharpFunctionalExtensions;
using DragonAssignmentDomain.Models;
using DragonAssignmentDomain.Poco;
using DragonCommonApplication;

namespace DragonAssignmentApplication.JobUpsert;

public static class JobCreateEditMapper
{
    public static Result<Job, JobValidationFailures> ToJob(JobCreateEdit input, IList<SkillTag> skillTags)
    {
        if (TryParseDates(input, out var startDate, out var endDate) is { } failures)
            return Result.Failure<Job, JobValidationFailures>(failures);

        var job = new Job
        {
            JobTitle = input.JobTitle,
            EmployerName = input.EmployerName,
            NumberOfPositions = input.NumberOfPositions,
            StartDate = startDate,
            EndDate = endDate,
            SkillTags = skillTags
        };
        return Result.Success<Job, JobValidationFailures>(job);
    }

    public static Result<Job, JobValidationFailures> ApplyTo(JobCreateEdit input, Job existing, IList<SkillTag> skillTags)
    {
        if (TryParseDates(input, out var startDate, out var endDate) is { } failures)
            return Result.Failure<Job, JobValidationFailures>(failures);

        existing.JobTitle = input.JobTitle;
        existing.EmployerName = input.EmployerName;
        existing.NumberOfPositions = input.NumberOfPositions;
        existing.StartDate = startDate;
        existing.EndDate = endDate;
        existing.SkillTags = skillTags;
        return Result.Success<Job, JobValidationFailures>(existing);
    }

    private static JobValidationFailures? TryParseDates(
        JobCreateEdit input, out DateTime startDate, out DateTime endDate)
    {
        var failures = new JobValidationFailures();

        if (!DateTime.TryParse(input.StartDate, out startDate))
            failures.StartDate = MappingMessages.MUST_BE_AN_ISO_DATE;
        if (!DateTime.TryParse(input.EndDate, out endDate))
            failures.EndDate = MappingMessages.MUST_BE_AN_ISO_DATE;

        if (failures.StartDate != null || failures.EndDate != null)
            return failures;

        return null;
    }
}
