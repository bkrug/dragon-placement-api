using CSharpFunctionalExtensions;
using DragonBilling.Domain.Enum;
using DragonCommon.Domain;
using DragonCommon.Domain.Poco;

namespace DragonBilling.Domain.Models;

public partial class WorkRequest
{
    public int WorkRequestId { get; set; }
    public int CustomerId { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public WorkRequestStatus WorkRequestStatus { get; set; } = WorkRequestStatus.Draft;

    /// <summary>
    /// At the time that the customer made the requet,
    /// estimated start and end dates represent their best guess as to how long work would last.
    /// This is hitorical information.
    /// After an asociated Job is created, it's start and end date might change while these fields might not.
    /// </summary>
    public DateTime? EstimatedStartDate { get; set; }
    public DateTime? EstimatedEndDate { get; set; }
    /// <summary>
    /// This too is historical information.
    /// It is useful as source data when we create a Job object in the Asignment Domain,
    /// but the models are not meant to be kept in sync.
    /// </summary>
    public int EstimatedWorkforceSize { get; set; }

    public virtual Customer Customer { get; set; } = null!;
    public ICollection<ChargeRate> ChargeRates { get; set; } = [];

    public Result<WorkRequest, ValidationFailures> Validate()
    {
        Dictionary<string, string> failures =
            new List<(string, string)>()
            {
                ( nameof(EstimatedWorkforceSize), ValidateEstimatedWorkforceSize() ),
                ( nameof(EstimatedEndDate), ValidateEstimatedEndDate() )
            }
            .Where(tuple => tuple.Item2 != string.Empty)
            .ToDictionary(tuple => tuple.Item1, tuple => tuple.Item2);

        return failures.Count == 0
            ? Result.Success<WorkRequest, ValidationFailures>(this)
            : Result.Failure<WorkRequest, ValidationFailures>(new ValidationFailures { FieldFailures = failures });
    }

    private string ValidateEstimatedWorkforceSize() =>
        EstimatedWorkforceSize < 0 ? ValidationMessages.MUST_BE_A_NON_NEGATIVE_NUMBER : string.Empty;

    private string ValidateEstimatedEndDate() =>
        EstimatedStartDate.HasValue && EstimatedEndDate.HasValue && EstimatedEndDate < EstimatedStartDate
            ? "must be later than start date"
            : string.Empty;
}
