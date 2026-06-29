using CSharpFunctionalExtensions;
using DragonAssignmentDomain.Models;
using DragonCommonApplication;
using DragonCommonDomain.Poco;

namespace DragonAssignmentApplication.JobUpsert;

public static class JobCreateEditMapper
{
    public static Result<Job, ValidationFailures> ToJob(JobCreateEdit input, IList<SkillTag> skillTags)
    {
        var parsingFailures = TryParseDates(input, out var startDate, out var endDate);
        if (parsingFailures.FieldFailures.Count > 0)
            return Result.Failure<Job, ValidationFailures>(parsingFailures);

        var job = new Job
        {
            JobTitle = input.JobTitle,
            EmployerName = input.EmployerName,
            NumberOfPositions = input.NumberOfPositions,
            StartDate = startDate,
            EndDate = endDate,
            SkillTags = skillTags
        };
        return Result.Success<Job, ValidationFailures>(job);
    }

    public static Result<Job, ValidationFailures> ApplyTo(JobCreateEdit input, Job existing, IList<SkillTag> skillTags)
    {
        var parsingFailures = TryParseDates(input, out var startDate, out var endDate);
        if (parsingFailures.FieldFailures.Count > 0)
            return Result.Failure<Job, ValidationFailures>(parsingFailures);

        existing.JobTitle = input.JobTitle;
        existing.EmployerName = input.EmployerName;
        existing.NumberOfPositions = input.NumberOfPositions;
        existing.StartDate = startDate;
        existing.EndDate = endDate;
        existing.SkillTags = skillTags;
        return Result.Success<Job, ValidationFailures>(existing);
    }

    private static ValidationFailures TryParseDates(
        JobCreateEdit input, out DateTime startDate, out DateTime endDate)
    {
        var fieldFailures = new Dictionary<string, string>();

        if (!DateTime.TryParse(input.StartDate, out startDate))
            fieldFailures[nameof(Job.StartDate)] = MappingMessages.MUST_BE_AN_ISO_DATE;
        if (!DateTime.TryParse(input.EndDate, out endDate))
            fieldFailures[nameof(Job.EndDate)] = MappingMessages.MUST_BE_AN_ISO_DATE;

        return new ValidationFailures { FieldFailures = fieldFailures };
    }
}
