using CSharpFunctionalExtensions;
using DragonAssignmentDomain.Poco;
using DragonCommonDomain;

namespace DragonAssignmentDomain.Models;

public class JobValidation
{
    public static Result<Job, JobValidationFailures> Validate(Job job)
    {
        var failures = new JobValidationFailures();

        if (string.IsNullOrWhiteSpace(job.JobTitle))
            failures.JobTitle = ValidationMessages.IS_REQUIRED;
        if (job.NumberOfPositions <= 0)
            failures.NumberOfPositions = ValidationMessages.MUST_BE_A_POSITIVE_NUMBER;
        if (job.StartDate.TimeOfDay != TimeSpan.Zero)
            failures.StartDate = ValidationMessages.MUST_BE_MIDNIGHT_UTC;
        if (job.EndDate.TimeOfDay != TimeSpan.Zero)
            failures.EndDate = ValidationMessages.MUST_BE_MIDNIGHT_UTC;

        if (failures.JobTitle != null || failures.NumberOfPositions != null
            || failures.StartDate != null || failures.EndDate != null)
            return Result.Failure<Job, JobValidationFailures>(failures);

        return Result.Success<Job, JobValidationFailures>(job);
    }
}
