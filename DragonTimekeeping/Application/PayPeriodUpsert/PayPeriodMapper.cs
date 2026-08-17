using CSharpFunctionalExtensions;
using DragonCommon.Application;
using DragonCommon.Domain.Poco;
using DragonTimekeeping.Domain.Models;

namespace DragonTimekeeping.Application.PayPeriodUpsert;

public static class PayPeriodMapper
{
    public static Result<PayPeriod, ValidationFailures> ToPayPeriodModel(PayPeriodCreateEdit input)
    {
        var failures = new ValidationFailures();

        if (!DateTime.TryParse(input.StartDate, out var parsedStart))
            failures.FieldFailures[nameof(PayPeriod.StartDate)] = MappingMessages.MUST_BE_AN_ISO_DATE;

        if (!DateTime.TryParse(input.EndDate, out var parsedEnd))
            failures.FieldFailures[nameof(PayPeriod.EndDate)] = MappingMessages.MUST_BE_AN_ISO_DATE;

        List<(HoursWorked?, GridRowValidationFailures?)> parsedHoursWorked = input.HoursWorked
            .Select((HoursWorkedCreateEdit hw, int index) => ParseHoursWorked(hw, index))
            .ToList();
        var hwFailures = parsedHoursWorked
            .Where(tuple => tuple.Item2 != null)
            .Select(tuple => tuple.Item2!)
            .ToList();
        if (hwFailures.Count > 0)
            failures.GridRowFailures[nameof(PayPeriod.HoursWorked)] = hwFailures;

        if (failures.FieldFailures.Count > 0 || failures.GridRowFailures.Count > 0)
        {
            return Result.Failure<PayPeriod, ValidationFailures>(failures);
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
            return Result.Success<PayPeriod, ValidationFailures>(payPeriod);
        }
    }

    private static (HoursWorked? transformed, GridRowValidationFailures? failure)
        ParseHoursWorked(HoursWorkedCreateEdit hw, int index)
    {
        var hwFailures = new GridRowValidationFailures
        {
            Index = index
        };
        DateTime parsedHwStart = DateTime.MinValue;
        DateTime parsedHwEnd = DateTime.MinValue;

        if (string.IsNullOrEmpty(hw.StartDateTime))
            hwFailures.FieldFailures[nameof(HoursWorked.StartDateTime)] = MappingMessages.IS_REQUIRED;
        else if (!DateTime.TryParse(hw.StartDateTime, out parsedHwStart))
            hwFailures.FieldFailures[nameof(HoursWorked.StartDateTime)] = MappingMessages.MUST_BE_AN_ISO_DATE;

        if (string.IsNullOrEmpty(hw.EndDateTime))
            hwFailures.FieldFailures[nameof(HoursWorked.EndDateTime)] = MappingMessages.IS_REQUIRED;
        else if (!DateTime.TryParse(hw.EndDateTime, out parsedHwEnd))
            hwFailures.FieldFailures[nameof(HoursWorked.EndDateTime)] = MappingMessages.MUST_BE_AN_ISO_DATE;

        if (hwFailures.FieldFailures.Count == 0)
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
