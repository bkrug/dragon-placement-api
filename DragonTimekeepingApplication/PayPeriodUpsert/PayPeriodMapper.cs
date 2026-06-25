using CSharpFunctionalExtensions;
using DragonCommonApplication;
using DragonTimekeepingDomain.Models;
using DragonTimekeepingDomain.Validation;

namespace DragonTimekeepingApplication.PayPeriodUpsert;

public static class PayPeriodMapper
{
    public static Result<PayPeriod, PayPeriodValidationFailures> ToPayPeriodModel(PayPeriodCreateEdit input)
    {
        var failures = new PayPeriodValidationFailures();

        if (!DateTime.TryParse(input.StartDate, out var parsedStart))
            failures.StartDate = MappingMessages.MUST_BE_AN_ISO_DATE;
        // else if (parsedStart.TimeOfDay.TotalSeconds != 0)
        //     failures.StartDate = "must exclude time-of-day or be midnight UTC";

        if (!DateTime.TryParse(input.EndDate, out var parsedEnd))
            failures.EndDate = MappingMessages.MUST_BE_AN_ISO_DATE;
        // else if (parsedEnd.TimeOfDay.TotalSeconds != 0)
        //     failures.StartDate = "must exclude time-of-day or be midnight UTC";

        List<(HoursWorked?, HoursWorkedValidationFailures?)> parsedHoursWorked = input.HoursWorked
            .Select((HoursWorkedCreateEdit hw, int index) => ParseHoursWorked(hw, index))
            .ToList();
        failures.HoursWorked = parsedHoursWorked
            .Where(tuple => tuple.Item2 != null)
            .Select(tuple => tuple.Item2!)
            .ToList();

        if (!string.IsNullOrEmpty(failures.StartDate)
            || !string.IsNullOrEmpty(failures.EndDate)
            || failures.HoursWorked.Count > 0)
        {
            return Result.Failure<PayPeriod, PayPeriodValidationFailures>(failures);
        }
        else
        {
            var payPeriod = new PayPeriod
            {
                AssignmentId = input.AssignmentId,
                StartDate = parsedStart,
                EndDate = parsedEnd,
                SubmissionStatus = input.SubmissionStatus,
                HoursWorked = parsedHoursWorked.Select(tuple => tuple.Item1!).ToList()
            };
            return Result.Success<PayPeriod, PayPeriodValidationFailures>(payPeriod);
        }
    }

    private static (HoursWorked? transformed, HoursWorkedValidationFailures? failure) 
        ParseHoursWorked(HoursWorkedCreateEdit hw, int index)
    {
        var hwFailures = new HoursWorkedValidationFailures
        {
            Index = index
        };
        DateTime parsedHwStart = DateTime.MinValue;
        DateTime parsedHwEnd = DateTime.MinValue;

        if (string.IsNullOrEmpty(hw.StartDateTime))
            hwFailures.StartDateTime = MappingMessages.IS_REQUIRED;
        else if (!DateTime.TryParse(hw.StartDateTime, out parsedHwStart))
            hwFailures.StartDateTime = MappingMessages.MUST_BE_AN_ISO_DATE;

        if (string.IsNullOrEmpty(hw.EndDateTime))
            hwFailures.EndDateTime = MappingMessages.IS_REQUIRED;
        else if (!DateTime.TryParse(hw.EndDateTime, out parsedHwEnd))
            hwFailures.EndDateTime = MappingMessages.MUST_BE_AN_ISO_DATE;

        if (string.IsNullOrEmpty(hwFailures.StartDateTime) && string.IsNullOrEmpty(hwFailures.EndDateTime))
        {
            var hwModel = new HoursWorked
            {
                StartDateTime = parsedHwStart,
                EndDateTime = parsedHwEnd
            };
            return new(hwModel, null);
        }
        else
        {
            return new(null, hwFailures);
        }
    }
}
