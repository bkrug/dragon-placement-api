using DragonAssignmentDomain.Models;

namespace DragonAssignmentApplication.JobUpsert;

public static class JobCreateEditMapper
{
    public static Job ToJob(JobCreateEdit input, IList<SkillTag> skillTags)
    {
        return new Job
        {
            JobTitle = input.JobTitle,
            EmployerName = input.EmployerName,
            NumberOfPositions = input.NumberOfPositions,
            StartDate = DateTimeOffset.FromUnixTimeSeconds(input.StartDateUnix).UtcDateTime,
            EndDate = DateTimeOffset.FromUnixTimeSeconds(input.EndDateUnix).UtcDateTime,
            SkillTags = skillTags
        };
    }

    public static void ApplyTo(JobCreateEdit input, Job existing, IList<SkillTag> skillTags)
    {
        existing.JobTitle = input.JobTitle;
        existing.EmployerName = input.EmployerName;
        existing.NumberOfPositions = input.NumberOfPositions;
        existing.StartDate = DateTimeOffset.FromUnixTimeSeconds(input.StartDateUnix).UtcDateTime;
        existing.EndDate = DateTimeOffset.FromUnixTimeSeconds(input.EndDateUnix).UtcDateTime;
        existing.SkillTags = skillTags;
    }
}
