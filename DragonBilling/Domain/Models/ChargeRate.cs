namespace DragonBilling.Domain.Models;

public partial class ChargeRate
{
    public int ChargeRateId { get; set; }
    public int AssignmentId { get; set; }
    public decimal HourlyRate { get; set; }
}
