namespace DragonPlacementApi.Poco;

public class HoursWorkedCreateEditNew
{
    public string StartDateTime { get; set; } = string.Empty;
    public string EndDateTime { get; set; } = string.Empty;
}

public class PayPeriodCreateEditNew
{
    public int AssignmentId { get; set; }
    public int DragonId { get; set; }
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public string SubmissionStatus { get; set; } = null!;
    public IList<HoursWorkedCreateEditNew> HoursWorked { get; set; } = [];
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

public class HoursWorkedValidationFailures : GridRowValidationFailures
{
    public string StartDateTime { get; set; } = string.Empty;
    public string EndDateTime { get; set; } = string.Empty;
}

public class PayPeriodValidationFailuresNew
{
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public IList<HoursWorkedValidationFailures> HoursWorked { get; set; } = [];
}

public class ValidPaySpan
{
    public string StartDate { get;set; } = string.Empty;
    public string EndDate { get;set; } = string.Empty;
}