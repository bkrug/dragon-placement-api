namespace DragonTimekeepingApplication.PayPeriodUpsert;

public class HoursWorkedCreateEdit
{
    public string StartDateTime { get; set; } = string.Empty;
    public string EndDateTime { get; set; } = string.Empty;
}

public class PayPeriodCreateEdit
{
    public int AssignmentId { get; set; }
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public string SubmissionStatus { get; set; } = null!;
    public IList<HoursWorkedCreateEdit> HoursWorked { get; set; } = [];
}
