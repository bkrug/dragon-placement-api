using DragonTimekeepingDomain.Models;
using CSharpFunctionalExtensions;
using DragonTimekeepingDomain.Poco;

namespace DragonTimekeepingDomain.Validation;

public static class PayPeriodValidation
{
    public static Result<PayPeriod, PayPeriodValidationFailures> Validate(PayPeriod payPeriod)
    {
        var parsedStart = payPeriod.StartDate;
        var parsedEnd = payPeriod.EndDate;

        Dictionary<string, string> failures = 
            new List<(string, string)>()
            {
                ( nameof(PayPeriod.StartDate), ValidateStartDate(parsedStart) ),
                ( nameof(PayPeriod.EndDate), ValidateEndDate(parsedEnd, parsedStart) )
            }
            .Where(tuple => tuple.Item2 != string.Empty)
            .ToDictionary(tuple => tuple.Item1, tuple => tuple.Item2);

        var hoursWorkedFailures = ValidateHoursWorked(payPeriod);

        if (failures.Count == 0 && hoursWorkedFailures.Count == 0) {
            return Result.Success<PayPeriod, PayPeriodValidationFailures>(payPeriod);
        }
        else {
            var validationFailures = new PayPeriodValidationFailures
            {
                StartDate = failures.GetValueOrDefault(nameof(PayPeriod.StartDate), string.Empty),
                EndDate = failures.GetValueOrDefault(nameof(PayPeriod.EndDate), string.Empty),
                HoursWorked = hoursWorkedFailures
            };
            return Result.Failure<PayPeriod, PayPeriodValidationFailures>(validationFailures);
        }
    }

    private static string ValidateStartDate(DateTime parsedStart)
    {
        if (parsedStart.DayOfWeek != DayOfWeek.Monday)
            return "must be a Monday";
        if (parsedStart.TimeOfDay.TotalSeconds != 0)
            return "must exclude time-of-day or be midnight UTC";
        return string.Empty;
    }

    private static string ValidateEndDate(DateTime parsedEnd, DateTime parsedStart)
    {
        if (parsedEnd.DayOfWeek != DayOfWeek.Sunday)
            return "must be a Sunday";
        if (parsedEnd.TimeOfDay.TotalSeconds != 0)
            return "must exclude time-of-day or be midnight UTC";
        if (parsedEnd <= parsedStart)
            return "must be greater than StartDate";
        return string.Empty;
    }

    private static IList<HoursWorkedValidationFailures> ValidateHoursWorked(PayPeriod payPeriod)
    {
        var payPeriodEndPlusOneDay = payPeriod.EndDate.AddDays(1);

        return payPeriod.HoursWorked
            .Select((hw, index) =>
            {
                string validationMessage = string.Empty;
                if (hw.StartDateTime < payPeriod.StartDate)
                    validationMessage = "Clock-in time is outside of the pay period";
                else if (hw.EndDateTime >= payPeriodEndPlusOneDay)
                    validationMessage = "Clock-out time is outside of the pay period";
                else if (payPeriod.HoursWorked.Where((other, i) => DoRecordsOverlap(hw, index, other, i)).Any())
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

    private static bool DoRecordsOverlap(HoursWorked reocrd1, int index1, HoursWorked record2, int index2)
    {
        return index2 != index1
            && reocrd1.StartDateTime < record2.EndDateTime
            && record2.StartDateTime < reocrd1.EndDateTime;
    }
}
