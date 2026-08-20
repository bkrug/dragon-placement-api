using CSharpFunctionalExtensions;
using DragonCommon.Domain.Poco;
using DragonTimekeeping.Domain.Enums;

namespace DragonTimekeeping.Domain.Models;

public class PayPeriod
{
    public int PayPeriodId { get; set; }
    public int AssignmentId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public PayPeriodStatus SubmissionStatus { get; set; } = PayPeriodStatus.Draft;

    public virtual ICollection<HoursWorked> HoursWorked { get; set; } = [];

    public void ApplyEdit(PayPeriod input)
    {
        var inputClockIns = input.HoursWorked.ToList();

        //Delete child records not found in input object
        var clockInsToDelete = HoursWorked
            .Where(existingHw => !inputClockIns.Any(ih => ih.StartDateTime == existingHw.StartDateTime))
            .ToList();
        foreach (var recToDelete in clockInsToDelete)
            HoursWorked.Remove(recToDelete);

        //Update fields in this object
        AssignmentId = input.AssignmentId;
        StartDate = input.StartDate;
        EndDate = input.EndDate;

        //Insert and update child records coming from input object.
        foreach (var inputClockIn in inputClockIns)
        {
            var existingClockPunch = HoursWorked.FirstOrDefault(h => h.StartDateTime == inputClockIn.StartDateTime);
            if (existingClockPunch == null)
                HoursWorked.Add(inputClockIn);
            else
                existingClockPunch.EndDateTime = inputClockIn.EndDateTime;
        }
    }

    public Result<PayPeriod, ValidationFailures> Validate()
    {
        Dictionary<string, string> failures =
            new List<(string, string)>()
            {
                ( nameof(StartDate), ValidateStartDate() ),
                ( nameof(EndDate), ValidateEndDate() )
            }
            .Where(tuple => tuple.Item2 != string.Empty)
            .ToDictionary(tuple => tuple.Item1, tuple => tuple.Item2);

        var hoursWorkedFailures = ValidateHoursWorked();

        if (failures.Count == 0 && hoursWorkedFailures.Count == 0) {
            return Result.Success<PayPeriod, ValidationFailures>(this);
        }
        else {
            var validationFailures = new ValidationFailures { FieldFailures = failures };
            if (hoursWorkedFailures.Count > 0)
                validationFailures.GridRowFailures[nameof(HoursWorked)] = hoursWorkedFailures;
            return Result.Failure<PayPeriod, ValidationFailures>(validationFailures);
        }
    }

    private string ValidateStartDate()
    {
        if (StartDate.DayOfWeek != DayOfWeek.Monday)
            return "must be a Monday";
        if (StartDate.TimeOfDay.TotalSeconds != 0)
            return "must exclude time-of-day or be midnight UTC";
        return string.Empty;
    }

    private string ValidateEndDate()
    {
        if (EndDate.DayOfWeek != DayOfWeek.Sunday)
            return "must be a Sunday";
        if (EndDate.TimeOfDay.TotalSeconds != 0)
            return "must exclude time-of-day or be midnight UTC";
        if (EndDate <= StartDate)
            return "must be greater than StartDate";
        return string.Empty;
    }

    private List<GridRowValidationFailures> ValidateHoursWorked()
    {
        var payPeriodEndPlusOneDay = EndDate.AddDays(1);

        return HoursWorked
            .Select((hw, index) =>
            {
                string validationMessage = string.Empty;
                if (hw.StartDateTime < StartDate)
                    validationMessage = "Clock-in time is outside of the pay period";
                else if (hw.EndDateTime >= payPeriodEndPlusOneDay)
                    validationMessage = "Clock-out time is outside of the pay period";
                else if (HoursWorked.Where((other, i) => DoRecordsOverlap(hw, index, other, i)).Any())
                    validationMessage = "Overlaps with another hours-worked record";
                return (index, validationMessage);
            })
            .Where(tuple => tuple.validationMessage != string.Empty)
            .Select(tuple => new GridRowValidationFailures
            {
                Index = tuple.index,
                RowValidationMessage = tuple.validationMessage
            })
            .ToList();
    }

    private static bool DoRecordsOverlap(HoursWorked record1, int index1, HoursWorked record2, int index2)
    {
        return index2 != index1
            && record1.StartDateTime < record2.EndDateTime
            && record2.StartDateTime < record1.EndDateTime;
    }
}
