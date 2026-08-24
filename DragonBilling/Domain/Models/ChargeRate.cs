namespace DragonBilling.Domain.Models;

public partial class ChargeRate
{
    public int ChargeRateId { get; set; }
    public int WorkRequestId { get; set; }
    public decimal HourlyRate { get; set; }

    public virtual WorkRequest WorkRequest { get; set; } = null!;
    public ICollection<WorkRequest> BillableHours { get; set; } = [];
}
