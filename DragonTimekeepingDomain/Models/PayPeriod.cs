namespace DragonTimekeepingDomain.Models;

public partial class PayPeriod
{
    public int PayPeriodId { get; set; }
    public int AssignmentId { get; set; }
    public long StartDateUnix { get; set; }
    public long EndDateUnix { get; set; }
    public string SubmissionStatus { get; set; } = null!;

    public virtual ICollection<HoursWorked> HoursWorked { get; set; } = [];
}
