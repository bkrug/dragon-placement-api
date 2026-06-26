using DragonCommonDomain.Poco;

namespace DragonTimekeepingDomain.Poco;

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
