namespace DragonTimekeepingDomain.Models;

public class PayPeriod
{
    public int PayPeriodId { get; set; }
    public int AssignmentId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string SubmissionStatus { get; set; } = null!;

    public ICollection<HoursWorked> HoursWorked { get; set; } = [];
}
