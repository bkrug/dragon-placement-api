namespace DragonTimekeepingDomain.Models;

public partial class HoursWorked
{
    public int HoursWorkedId { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public int PayPeriodId { get; set; }

    public virtual PayPeriod PayPeriod { get; set; } = null!;
}
