namespace DragonPlacementDataLayer.Models;

public partial class PayPeriod
{
    public int PayPeriodId { get; set; }
    public int AssignmentId { get; set; }
    public int DragonId { get; set; }
    public long StartDateUnix { get; set; }
    public long EndDateUnix { get; set; }
    public string SubmissionStatus { get; set; } = null!;

    public virtual Assignment Assignment { get; set; } = null!;
    public virtual ICollection<HoursWorked> HoursWorked { get; set; } = [];
}
