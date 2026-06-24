using DragonAssignmentDomain.Poco;
using CSharpFunctionalExtensions;

namespace DragonAssignmentDomain.Models;

public partial class Job
{
    public JobValidationFailures? Validate()
    {
        var failures = new JobValidationFailures();

        if (string.IsNullOrWhiteSpace(JobTitle))
            failures.JobTitle = "is required";
        if (NumberOfPositions <= 0)
            failures.NumberOfPositions = "must be a positive number";
        if (StartDate.TimeOfDay != TimeSpan.Zero)
            failures.StartDateUnix = "must be midnight UTC";
        if (EndDate.TimeOfDay != TimeSpan.Zero)
            failures.EndDateUnix = "must be midnight UTC";

        if (failures.JobTitle != null || failures.NumberOfPositions != null
            || failures.StartDateUnix != null || failures.EndDateUnix != null)
            return failures;

        return null;
    }
}
