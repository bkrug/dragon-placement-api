using DragonTimekeepingDomain.Models;

namespace DragonPlacementTests.PayPeriodTests;

public class PayPeriodBuilder
{
    private static readonly DateTime EPOCH = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private int _payPeriodId;
    private int _assignmentId = 10;
    private DateTime _startDate = EPOCH.AddDays(1);
    private DateTime _endDate = EPOCH.AddDays(8);
    private string _submissionStatus = "Draft";
    private List<HoursWorked> _hoursWorked = [];

    public PayPeriodBuilder WithPayPeriodId(int id) { _payPeriodId = id; return this; }
    public PayPeriodBuilder WithAssignmentId(int id) { _assignmentId = id; return this; }
    public PayPeriodBuilder WithStartDate(DateTime val) { _startDate = val; return this; }
    public PayPeriodBuilder WithStartDate(int daysFromEpoch) {
        _startDate = EPOCH.AddDays(daysFromEpoch);
        return this;
    }
    public PayPeriodBuilder WithEndDate(DateTime val) { _endDate = val; return this; }
    public PayPeriodBuilder WithEndDate(int daysFromEpoch) {
        _endDate = EPOCH.AddDays(daysFromEpoch);
        return this;
    }
    public PayPeriodBuilder WithSubmissionStatus(string val) { _submissionStatus = val; return this; }
    public PayPeriodBuilder AddHoursWorked(DateTime clockIn, DateTime clockOut)
    {
        return AddHoursWorked(0, clockIn, clockOut);
    }
    public PayPeriodBuilder AddHoursWorked(int hoursWorkedId, DateTime clockIn, DateTime clockOut)
    {
        _hoursWorked.Add(new HoursWorked
        {
            HoursWorkedId = hoursWorkedId,
            StartDateTime = clockIn,
            EndDateTime = clockOut
        });
        return this;
    }
    /// <param name="clockInOffset">A TimeSpan relative to the pay period start</param>
    /// <param name="clockOutOffset">A TimeSpan relative to the pay period start</param>
    public PayPeriodBuilder AddHoursWorkedRelative(TimeSpan clockInOffset, TimeSpan clockOutOffset)
    {
        return AddHoursWorkedRelative(0, clockInOffset, clockOutOffset);
    }
    /// <param name="clockInOffset">A TimeSpan relative to the pay period start</param>
    /// <param name="clockOutOffset">A TimeSpan relative to the pay period start</param>
    public PayPeriodBuilder AddHoursWorkedRelative(int hoursWorkedId, TimeSpan clockInOffset, TimeSpan clockOutOffset)
    {
        _hoursWorked.Add(new HoursWorked
        {
            HoursWorkedId = hoursWorkedId,
            StartDateTime = _startDate + clockInOffset,
            EndDateTime = _startDate + clockOutOffset
        });
        return this;
    }

    public PayPeriod Build() => new()
    {
        PayPeriodId = _payPeriodId,
        AssignmentId = _assignmentId,
        StartDate = _startDate,
        EndDate = _endDate,
        SubmissionStatus = _submissionStatus,
        HoursWorked = _hoursWorked
    };
}
