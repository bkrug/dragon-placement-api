namespace TimekeepingDataLayer.Models;

public partial class HoursWorked
{
    public int HoursWorkedId { get; set; }
    public long StartDateTimeUnix { get; set; }
    public long EndDateTimeUnix { get; set; }
    public int PayPeriodId { get; set; }

    public virtual PayPeriod PayPeriod { get; set; } = null!;
}
