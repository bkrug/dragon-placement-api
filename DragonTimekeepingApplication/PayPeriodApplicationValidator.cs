using DragonTimekeepingApplication.Dto;
using DragonTimekeepingDomain.Models;
using DragonTimekeepingDomain.Validation;

namespace DragonTimekeepingApplication;

public static class PayPeriodApplicationValidator
{
    public static (PayPeriod? PayPeriod, PayPeriodValidationFailures? Failures) GetPayPeriodModel(PayPeriodCreateEdit input)
    {
        var (payPeriod, parsingFailures) = TransformPayPeriod(input);

        if (parsingFailures == null) {
            var domainFailures = PayPeriodDomainValidator.Validate(payPeriod);
            return (
                domainFailures == null ? payPeriod : null,
                domainFailures
            );
        }
        else
        {
            return (payPeriod, parsingFailures);
        }
    }

    public static (PayPeriod? PayPeriod, PayPeriodValidationFailures? Failures) TransformPayPeriod(PayPeriodCreateEdit input)
    {
        var failures = new PayPeriodValidationFailures();

        if (!DateTime.TryParse(input.StartDate, out var parsedStart))
            failures.StartDate = "must be an ISO Date";
        // else if (parsedStart.TimeOfDay.TotalSeconds != 0)
        //     failures.StartDate = "must exclude time-of-day or be midnight UTC";

        if (!DateTime.TryParse(input.EndDate, out var parsedEnd))
            failures.EndDate = "must be an ISO Date";
        // else if (parsedEnd.TimeOfDay.TotalSeconds != 0)
        //     failures.StartDate = "must exclude time-of-day or be midnight UTC";

        if (!string.IsNullOrEmpty(failures.StartDate) || !string.IsNullOrEmpty(failures.EndDate))
            return (null, failures);

        //TODO: Add validation that these strings parsed correctly
        var parsedHoursWorked = input.HoursWorked
            .Select(hw => (
                Start: DateTime.Parse(hw.StartDateTime),
                End: DateTime.Parse(hw.EndDateTime)
            ))
            .ToList();

        var payPeriod = new PayPeriod
        {
            AssignmentId = input.AssignmentId,
            StartDate = parsedStart,
            EndDate = parsedEnd,
            SubmissionStatus = input.SubmissionStatus,
            HoursWorked = parsedHoursWorked.Select(hw => new HoursWorked
            {
                StartDateTimeUnix = new DateTimeOffset(hw.Start, TimeSpan.Zero).ToUnixTimeSeconds(),
                EndDateTimeUnix = new DateTimeOffset(hw.End, TimeSpan.Zero).ToUnixTimeSeconds()
            }).ToList()
        };
        return (payPeriod, null);
    }    
}
