namespace DragonTimekeepingDomain.Models;

public class HoursWorked
{
    public int HoursWorkedId { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public int PayPeriodId { get; set; }

    public PayPeriod PayPeriod { get; set; } = null!;
}
