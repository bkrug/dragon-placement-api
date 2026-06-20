namespace DragonPlacementApi.Poco;

public class HoursWorkedCreateEdit
{
    public long StartDateTimeUnix { get; set; }
    public long EndDateTimeUnix { get; set; }
}

public class PayPeriodCreateEdit
{
    public int AssignmentId { get; set; }
    public int DragonId { get; set; }
    public long StartDateUnix { get; set; }
    public long EndDateUnix { get; set; }
    public string SubmissionStatus { get; set; } = null!;
    public IList<HoursWorkedCreateEdit> HoursWorked { get; set; } = [];
}


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
    public int DragonId { get; set; }
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public string SubmissionStatus { get; set; } = null!;
    public IList<HoursWorkedView> HoursWorked { get; set; } = [];
}

public class ValidPaySpan
{
    public string StartDate { get;set; } = string.Empty;
    public string EndDate { get;set; } = string.Empty;
}
