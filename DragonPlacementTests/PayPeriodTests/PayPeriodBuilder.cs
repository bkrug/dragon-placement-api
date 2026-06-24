using DragonPlacementApi.Poco;
using DragonTimekeepingDomain.Models;

namespace DragonPlacementTests.PayPeriodTests;

public class PayPeriodBuilder
{
    private int _payPeriodId;
    private int _assignmentId = 10;
    private long _startDateUnix = 1 * Const.SECONDS_IN_A_DAY;
    private long _endDateUnix = 8 * Const.SECONDS_IN_A_DAY;
    private string _submissionStatus = "Draft";
    private List<HoursWorked> _hoursWorked = [];

    public PayPeriodBuilder WithPayPeriodId(int id) { _payPeriodId = id; return this; }
    public PayPeriodBuilder WithAssignmentId(int id) { _assignmentId = id; return this; }
    public PayPeriodBuilder WithStartDate(DateTime startDate) {
        _startDateUnix = new DateTimeOffset(startDate, TimeSpan.Zero).ToUnixTimeSeconds();
        return this;
    }
    public PayPeriodBuilder WithEndDate(DateTime endDate) {
        _endDateUnix = new DateTimeOffset(endDate, TimeSpan.Zero).ToUnixTimeSeconds();
        return this;
    }
    public PayPeriodBuilder WithSubmissionStatus(string val) { _submissionStatus = val; return this; }
    public PayPeriodBuilder AddHoursWorked(DateTime clockInSeconds, DateTime clockOutSeconds)
    {
        return AddHoursWorked(0, clockInSeconds, clockOutSeconds);
    }
    public PayPeriodBuilder AddHoursWorked(int hoursWorkedId, DateTime clockInSeconds, DateTime clockOutSeconds)
    {
        _hoursWorked.Add(new HoursWorked
        {
            HoursWorkedId = hoursWorkedId,
            StartDateTime = clockInSeconds,
            EndDateTime = clockOutSeconds 
        });
        return this;
    }    
    /// <param name="clockInSeconds">A number of seconds relative to the pay period start</param>
    /// <param name="clockOutSeconds">A number of seconds relative to the pay period start</param>
    /// <returns></returns>
    public PayPeriodBuilder AddHoursWorkedRelative(long clockInSeconds, long clockOutSeconds)
    {
        return AddHoursWorkedRelative(0, clockInSeconds, clockOutSeconds);
    }
    /// <param name="clockInSeconds">A number of seconds relative to the pay period start</param>
    /// <param name="clockOutSeconds">A number of seconds relative to the pay period start</param>
    /// <returns></returns>
    public PayPeriodBuilder AddHoursWorkedRelative(int hoursWorkedId, long clockInSeconds, long clockOutSeconds)
    {
        _hoursWorked.Add(new HoursWorked
        {
            HoursWorkedId = hoursWorkedId,
            StartDateTime = DateTimeOffset.FromUnixTimeSeconds(_startDateUnix + clockInSeconds).UtcDateTime,
            EndDateTime = DateTimeOffset.FromUnixTimeSeconds(_startDateUnix + clockOutSeconds).UtcDateTime
        });
        return this;
    }

    public PayPeriod Build() => new()
    {
        PayPeriodId = _payPeriodId,
        AssignmentId = _assignmentId,
        StartDate = DateTimeOffset.FromUnixTimeSeconds(_startDateUnix).UtcDateTime,
        EndDate = DateTimeOffset.FromUnixTimeSeconds(_endDateUnix).UtcDateTime,
        SubmissionStatus = _submissionStatus,
        HoursWorked = _hoursWorked
    };
}
