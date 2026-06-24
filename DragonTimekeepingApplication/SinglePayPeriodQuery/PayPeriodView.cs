namespace DragonTimekeepingApplication.SinglePayPeriodQuery;

public class HoursWorkedView
{
    public string StartDateTime { get; set; } = string.Empty;
    public string EndDateTime { get; set; } = string.Empty;
}

public class PayPeriodView
{
    public string AssignmentDescription { get; set; } = string.Empty;
    public string DragonName { get; set; } = string.Empty;
    public int AssignmentId { get; set; }
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public string SubmissionStatus { get; set; } = null!;
    public IList<HoursWorkedView> HoursWorked { get; set; } = [];
}
