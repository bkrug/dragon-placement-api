using CSharpFunctionalExtensions;
using DragonCommonDomain;
using DragonCommonDomain.Poco;

namespace DragonAssignment.Domain.Models;

public partial class Job
{
    public int JobId { get; set; }

    public string JobTitle { get; set; } = null!;

    public string? EmployerName { get; set; }

    public int NumberOfPositions { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public virtual ICollection<Assignment> Assignments { get; set; } = [];

    public virtual ICollection<SkillTag> SkillTags { get; set; } = [];

    public Assignment Assign(int dragonId)
    {
        var assignment = new Assignment
        {
            DragonId = dragonId,
            JobId = JobId,
            StartDate = StartDate,
            EndDate = EndDate
        };
        Assignments.Add(assignment);
        return assignment;
    }

    public Result<Job, ValidationFailures> Validate()
    {
        Dictionary<string, string> failures =
            new List<(string, string)>()
            {
                ( nameof(JobTitle), ValidateJobTitle() ),
                ( nameof(NumberOfPositions), ValidateNumberOfPositions() ),
                ( nameof(StartDate), ValidateStartDate() ),
                ( nameof(EndDate), ValidateEndDate() )
            }
            .Where(tuple => tuple.Item2 != string.Empty)
            .ToDictionary(tuple => tuple.Item1, tuple => tuple.Item2);

        return failures.Count == 0
            ? Result.Success<Job, ValidationFailures>(this)
            : Result.Failure<Job, ValidationFailures>(new ValidationFailures { FieldFailures = failures });
    }

    private string ValidateJobTitle() =>
        string.IsNullOrWhiteSpace(JobTitle) ? ValidationMessages.IS_REQUIRED : string.Empty;

    private string ValidateNumberOfPositions() =>
        NumberOfPositions <= 0 ? ValidationMessages.MUST_BE_A_POSITIVE_NUMBER : string.Empty;

    private string ValidateStartDate() =>
        StartDate.TimeOfDay != TimeSpan.Zero ? ValidationMessages.MUST_BE_MIDNIGHT_UTC : string.Empty;

    private string ValidateEndDate()
    {
        if (EndDate.TimeOfDay != TimeSpan.Zero)
            return ValidationMessages.MUST_BE_MIDNIGHT_UTC;
        //TODO: Consider moving this to some row-level or form-level validation failure message,
        // as it can be corrected by chaning either of two fields.
        if (EndDate < StartDate)
            return "must be later than start date";
        return string.Empty;
    }
}
