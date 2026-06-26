using CSharpFunctionalExtensions;
using DragonAssignmentDomain.Poco;
using DragonCommonDomain;

namespace DragonAssignmentDomain.Models;

public class JobValidation
{
    public static Result<Job, JobValidationFailures> Validate(Job job)
    {
        Dictionary<string, string> failures =
            new List<(string, string)>()
            {
                ( nameof(Job.JobTitle), ValidateJobTitle(job.JobTitle) ),
                ( nameof(Job.NumberOfPositions), ValidateNumberOfPositions(job.NumberOfPositions) ),
                ( nameof(Job.StartDate), ValidateDate(job.StartDate) ),
                ( nameof(Job.EndDate), ValidateDate(job.EndDate) )
            }
            .Where(tuple => tuple.Item2 != string.Empty)
            .ToDictionary(tuple => tuple.Item1, tuple => tuple.Item2);

        if (failures.Count == 0) {
            return Result.Success<Job, JobValidationFailures>(job);
        }
        else {
            var validationFailures = new JobValidationFailures
            {
                JobTitle = failures.GetValueOrDefault(nameof(Job.JobTitle), null!),
                NumberOfPositions = failures.GetValueOrDefault(nameof(Job.NumberOfPositions), null!),
                StartDate = failures.GetValueOrDefault(nameof(Job.StartDate), null!),
                EndDate = failures.GetValueOrDefault(nameof(Job.EndDate), null!)
            };
            return Result.Failure<Job, JobValidationFailures>(validationFailures);
        }
    }

    private static string ValidateJobTitle(string jobTitle) =>
        string.IsNullOrWhiteSpace(jobTitle) ? ValidationMessages.IS_REQUIRED : string.Empty;

    private static string ValidateNumberOfPositions(int numberOfPositions) =>
        numberOfPositions <= 0 ? ValidationMessages.MUST_BE_A_POSITIVE_NUMBER : string.Empty;

    private static string ValidateDate(DateTime date) =>
        date.TimeOfDay != TimeSpan.Zero ? ValidationMessages.MUST_BE_MIDNIGHT_UTC : string.Empty;
}
