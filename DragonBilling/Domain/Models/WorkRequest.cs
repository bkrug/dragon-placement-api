namespace DragonBilling.Domain.Models;

public partial class WorkRequest
{
    public int WorkRequestId { get; set; }
    public int CustomerId { get; set; }
    public string Name { get; set; } = null!;

    public virtual Customer Customer { get; set; } = null!;
    public ICollection<ChargeRate> ChargeRates { get; set; } = [];
}
