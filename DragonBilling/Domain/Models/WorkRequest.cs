using DragonBilling.Domain.Enum;

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
}
