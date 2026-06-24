namespace DragonTimekeepingDomain.Models;

public partial class PayPeriod
{
    public int PayPeriodId { get; set; }
    public int AssignmentId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string SubmissionStatus { get; set; } = null!;

    public virtual ICollection<HoursWorked> HoursWorked { get; set; } = [];
}
