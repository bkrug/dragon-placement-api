using DragonTimekeepingApplication.Dto;
using DragonTimekeepingDomain.Validation;

namespace DragonTimekeepingApplication;

public static class PayPeriodApplicationValidator
{
    public static PayPeriodValidationFailures? ValidatePayPeriod(PayPeriodCreateEdit input)
    {
        var failures = new PayPeriodValidationFailures();

        if (!DateTime.TryParse(input.StartDate, out var parsedStart))
            failures.StartDate = "must be an ISO Date";

        if (!DateTime.TryParse(input.EndDate, out var parsedEnd))
            failures.EndDate = "must be an ISO Date";

        if (!string.IsNullOrEmpty(failures.StartDate) || !string.IsNullOrEmpty(failures.EndDate))
            return failures;

        //TODO: Add validation that these strings parsed correctly
        var parsedHoursWorked = input.HoursWorked
            .Select(hw => (
                Start: DateTime.Parse(hw.StartDateTime),
                End: DateTime.Parse(hw.EndDateTime)
            ))
            .ToList();

        return PayPeriodDomainValidator.Validate(parsedStart, parsedEnd, parsedHoursWorked);
    }
}
