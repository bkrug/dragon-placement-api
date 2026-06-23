namespace DragonTimekeepingDomain.Validation;

//TODO: Move this class to some "CommonDomain" project.
public class GridRowValidationFailures
{
    public int Index { get;set; } = -1;
    public string RowValidationMessage { get; set; } = string.Empty;
}

public class HoursWorkedValidationFailures : GridRowValidationFailures
{
    public string StartDateTime { get; set; } = string.Empty;
    public string EndDateTime { get; set; } = string.Empty;
}

public class PayPeriodValidationFailures
{
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public IList<HoursWorkedValidationFailures> HoursWorked { get; set; } = [];
}
