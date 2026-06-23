namespace DragonTimekeepingDomain.Validation;

public static class PayPeriodValidator
{
    private const long SECONDS_IN_A_DAY = 86400;

    public static PayPeriodValidationFailures? Validate(
        DateTime parsedStart, DateTime parsedEnd,
        IList<(long StartUnix, long EndUnix)> hoursWorked)
    {
        var failures = new PayPeriodValidationFailures();

        if (parsedStart.DayOfWeek != DayOfWeek.Monday)
            failures.StartDate = "must be a Monday";
        else if (parsedStart.TimeOfDay.TotalSeconds != 0)
            failures.StartDate = "must exclude time-of-day or be midnight UTC";

        if (parsedEnd.DayOfWeek != DayOfWeek.Sunday)
            failures.EndDate = "must be a Sunday";
        else if (parsedEnd.TimeOfDay.TotalSeconds != 0)
            failures.EndDate = "must exclude time-of-day or be midnight UTC";
        else if (parsedEnd <= parsedStart)
            failures.EndDate = "must be greater than StartDate";

        long payPeriodStartUnix = new DateTimeOffset(parsedStart, TimeSpan.Zero).ToUnixTimeSeconds();
        long payPeriodEndUnix = new DateTimeOffset(parsedEnd, TimeSpan.Zero).ToUnixTimeSeconds();

        failures.HoursWorked = hoursWorked
            .Select((hw, index) =>
            {
                var hwf = new HoursWorkedValidationFailures()
                {
                    Index = index,
                };
                if (hw.StartUnix < payPeriodStartUnix)
                    hwf.RowValidationMessage = "Clock-in time is outside of the pay period";
                else if (hw.EndUnix >= payPeriodEndUnix + SECONDS_IN_A_DAY)
                    hwf.RowValidationMessage = "Clock-out time is outside of the pay period";
                else if (hoursWorked.Where((other, i) => i != index && hw.StartUnix < other.EndUnix && other.StartUnix < hw.EndUnix).Any())
                    hwf.RowValidationMessage = "Overlaps with another hours-worked record";
                return hwf;
            })
            .Where(hwf => !string.IsNullOrEmpty(hwf.StartDateTime) || !string.IsNullOrEmpty(hwf.EndDateTime) || !string.IsNullOrEmpty(hwf.RowValidationMessage))
            .ToList();

        return !string.IsNullOrEmpty(failures.StartDate)
            || !string.IsNullOrEmpty(failures.EndDate)
            || failures.HoursWorked.Count > 0
            ? failures
            : null;
    }
}
