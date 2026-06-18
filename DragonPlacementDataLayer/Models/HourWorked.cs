namespace DragonPlacementDataLayer.Models;

public partial class HoursWorked
{
    public int HoursWorkedId { get; set; }
    public int AssignmentId { get; set; }
    public int DragonId { get; set; }
    public long StartDateTimeUnix { get; set; }
    public long EndDateTimeUnix { get; set; }
    public int PayPeriodId { get; set; }

    public virtual Assignment Assignment { get; set; } = null!;
    public virtual PayPeriod PayPeriod { get; set; } = null!;
}
