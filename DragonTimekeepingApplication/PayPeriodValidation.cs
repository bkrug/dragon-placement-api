using DragonTimekeepingDomain.Validation;

namespace DragonTimekeepingApplication;

public static class PayPeriodValidation
{
    public static PayPeriodValidationFailures? ValidatePayPeriod(
        string startDate, string endDate,
        IList<(string StartDateTime, string EndDateTime)> hoursWorked)
    {
        var failures = new PayPeriodValidationFailures();

        if (!DateTime.TryParse(startDate, out var parsedStart))
            failures.StartDate = "must be an ISO Date";

        if (!DateTime.TryParse(endDate, out var parsedEnd))
            failures.EndDate = "must be an ISO Date";

        if (!string.IsNullOrEmpty(failures.StartDate) || !string.IsNullOrEmpty(failures.EndDate))
            return failures;

        //TODO: Add validation that these strings parsed correctly
        var parsedHoursWorked = hoursWorked
            .Select(hw => (
                Start: DateTime.Parse(hw.StartDateTime),
                End: DateTime.Parse(hw.EndDateTime)
            ))
            .ToList();

        return PayPeriodValidator.Validate(parsedStart, parsedEnd, parsedHoursWorked);
    }
}
