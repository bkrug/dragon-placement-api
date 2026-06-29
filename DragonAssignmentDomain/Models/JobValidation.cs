using CSharpFunctionalExtensions;
using DragonCommonDomain;
using DragonCommonDomain.Poco;

namespace DragonAssignmentDomain.Models;

public class JobValidation
{
    public static Result<Job, ValidationFailures> Validate(Job job)
    {
        Dictionary<string, string> failures =
            new List<(string, string)>()
            {
                ( nameof(Job.JobTitle), ValidateJobTitle(job.JobTitle) ),
                ( nameof(Job.NumberOfPositions), ValidateNumberOfPositions(job.NumberOfPositions) ),
                ( nameof(Job.StartDate), ValidateDate(job.StartDate) ),
                ( nameof(Job.EndDate), ValidateEndDate(job.EndDate, job.StartDate) )
            }
            .Where(tuple => tuple.Item2 != string.Empty)
            .ToDictionary(tuple => tuple.Item1, tuple => tuple.Item2);

        if (failures.Count == 0) {
            return Result.Success<Job, ValidationFailures>(job);
        }
        else {
            return Result.Failure<Job, ValidationFailures>(new ValidationFailures
            {
                FieldFailures = failures
            });
        }
    }

    private static string ValidateJobTitle(string jobTitle) =>
        string.IsNullOrWhiteSpace(jobTitle) ? ValidationMessages.IS_REQUIRED : string.Empty;

    private static string ValidateNumberOfPositions(int numberOfPositions) =>
        numberOfPositions <= 0 ? ValidationMessages.MUST_BE_A_POSITIVE_NUMBER : string.Empty;

    private static string ValidateDate(DateTime date) =>
        date.TimeOfDay != TimeSpan.Zero ? ValidationMessages.MUST_BE_MIDNIGHT_UTC : string.Empty;

    private static string ValidateEndDate(DateTime endDate, DateTime startDate)
    {
        if (endDate.TimeOfDay != TimeSpan.Zero)
            return ValidationMessages.MUST_BE_MIDNIGHT_UTC;
        //TODO: Consider moving this to some row-level or form-level validation failure message,
        // as it can be corrected by chaning either of two fields.
        if (endDate < startDate)
            return "must be later than start date";
        return string.Empty;
    }
}
