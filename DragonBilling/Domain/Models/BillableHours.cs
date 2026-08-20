namespace DragonBilling.Domain.Models;

public partial class BillableHours
{
    public int BillableHoursId { get; set; }
    public int ChargeRateId { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal TotalHours { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    public virtual ChargeRate ChargeRate { get; set; } = null!;

    public decimal CalculateTotalCharge()
    {
        return HourlyRate * TotalHours;
    }
}
