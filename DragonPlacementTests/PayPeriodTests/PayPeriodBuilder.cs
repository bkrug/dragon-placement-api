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
    public PayPeriodBuilder WithStartDateUnix(long unitCount, long secondsPerUnit) {
        _startDateUnix = unitCount * secondsPerUnit;
        return this;
    }
    public PayPeriodBuilder WithStartDateUnix(long val) { _startDateUnix = val; return this; }
    public PayPeriodBuilder WithEndDateUnix(long unitCount, long secondsPerUnit) {
        _endDateUnix = unitCount * secondsPerUnit;
        return this;
    }
    public PayPeriodBuilder WithEndDateUnix(long val) { _endDateUnix = val; return this; }
    public PayPeriodBuilder WithSubmissionStatus(string val) { _submissionStatus = val; return this; }
    public PayPeriodBuilder AddHoursWorked(long clockInSeconds, long clockOutSeconds)
    {
        return AddHoursWorked(0, clockInSeconds, clockOutSeconds);
    }
    public PayPeriodBuilder AddHoursWorked(int hoursWorkedId, long clockInSeconds, long clockOutSeconds)
    {
        _hoursWorked.Add(new HoursWorked
        {
            HoursWorkedId = hoursWorkedId,
            StartDateTimeUnix = clockInSeconds,
            EndDateTimeUnix = clockOutSeconds 
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
            StartDateTimeUnix = _startDateUnix + clockInSeconds,
            EndDateTimeUnix = _startDateUnix + clockOutSeconds 
        });
        return this;
    }

    public PayPeriod Build() => new()
    {
        PayPeriodId = _payPeriodId,
        AssignmentId = _assignmentId,
        StartDate = DateTimeOffset.FromUnixTimeSeconds(_startDateUnix).UtcDateTime,
        EndDateUnix = _endDateUnix,
        SubmissionStatus = _submissionStatus,
        HoursWorked = _hoursWorked
    };
}
