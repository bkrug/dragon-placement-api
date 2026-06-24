using CSharpFunctionalExtensions;
using DragonAssignmentDomain.Poco;

namespace DragonAssignmentDomain.Models;

public class JobValidation
{
    public static Result<Job, JobValidationFailures> Validate(Job job)
    {
        var failures = new JobValidationFailures();

        if (string.IsNullOrWhiteSpace(job.JobTitle))
            failures.JobTitle = "is required";
        if (job.NumberOfPositions <= 0)
            failures.NumberOfPositions = "must be a positive number";
        if (job.StartDate.TimeOfDay != TimeSpan.Zero)
            failures.StartDate = "must be midnight UTC";
        if (job.EndDate.TimeOfDay != TimeSpan.Zero)
            failures.EndDate = "must be midnight UTC";

        if (failures.JobTitle != null || failures.NumberOfPositions != null
            || failures.StartDate != null || failures.EndDate != null)
            return Result.Failure<Job, JobValidationFailures>(failures);

        return Result.Success<Job, JobValidationFailures>(job);
    }
}
