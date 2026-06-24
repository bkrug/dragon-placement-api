using DragonTimekeepingDomain.Models;

namespace DragonTimekeepingDomain.Validation;

public static class PayPeriodDomainValidator
{
    public static PayPeriodValidationFailures? Validate(PayPeriod payPeriod)
    {
        var parsedStart = DateTimeOffset.FromUnixTimeSeconds(payPeriod.StartDate).UtcDateTime;
        var parsedEnd = DateTimeOffset.FromUnixTimeSeconds(payPeriod.EndDateUnix).UtcDateTime;

        var failures = new Dictionary<string, string>();

        var startDateMessage = ValidateStartDate(parsedStart);
        if (startDateMessage != null)
            failures["StartDate"] = startDateMessage;

        var endDateMessage = ValidateEndDate(parsedEnd, parsedStart);
        if (endDateMessage != null)
            failures["EndDate"] = endDateMessage;

        var hoursWorked = payPeriod.HoursWorked
            .Select(hw => (
                Start: DateTimeOffset.FromUnixTimeSeconds(hw.StartDateTimeUnix).UtcDateTime,
                End: DateTimeOffset.FromUnixTimeSeconds(hw.EndDateTimeUnix).UtcDateTime
            ))
            .ToList();

        var hoursWorkedFailures = ValidateHoursWorked(hoursWorked, parsedStart, parsedEnd);

        if (failures.Count == 0 && hoursWorkedFailures.Count == 0)
            return null;

        return new PayPeriodValidationFailures
        {
            StartDate = failures.GetValueOrDefault("StartDate", string.Empty),
            EndDate = failures.GetValueOrDefault("EndDate", string.Empty),
            HoursWorked = hoursWorkedFailures
        };
    }

    private static string? ValidateStartDate(DateTime parsedStart)
    {
        if (parsedStart.DayOfWeek != DayOfWeek.Monday)
            return "must be a Monday";
        if (parsedStart.TimeOfDay.TotalSeconds != 0)
            return "must exclude time-of-day or be midnight UTC";
        return null;
    }

    private static string? ValidateEndDate(DateTime parsedEnd, DateTime parsedStart)
    {
        if (parsedEnd.DayOfWeek != DayOfWeek.Sunday)
            return "must be a Sunday";
        if (parsedEnd.TimeOfDay.TotalSeconds != 0)
            return "must exclude time-of-day or be midnight UTC";
        if (parsedEnd <= parsedStart)
            return "must be greater than StartDate";
        return null;
    }

    private static IList<HoursWorkedValidationFailures> ValidateHoursWorked(
        IList<(DateTime Start, DateTime End)> hoursWorked,
        DateTime payPeriodStart, DateTime payPeriodEnd)
    {
        var payPeriodEndPlusOneDay = payPeriodEnd.AddDays(1);

        return hoursWorked
            .Select((hw, index) =>
            {
                string validationMessage = string.Empty;
                if (hw.Start < payPeriodStart)
                    validationMessage = "Clock-in time is outside of the pay period";
                else if (hw.End >= payPeriodEndPlusOneDay)
                    validationMessage = "Clock-out time is outside of the pay period";
                else if (hoursWorked.Where((other, i) => i != index && hw.Start < other.End && other.Start < hw.End).Any())
                    validationMessage = "Overlaps with another hours-worked record";
                return (index, validationMessage);
            })
            .Where(tuple => tuple.validationMessage != string.Empty)
            .Select(tuple => new HoursWorkedValidationFailures
            {
                Index = tuple.index,
                RowValidationMessage = tuple.validationMessage
            })
            .ToList();
    }
}
