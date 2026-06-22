using DragonPlacementApi.Poco;
using TimekeepingDataLayer.Models;

namespace DragonPlacementTests.PayPeriodTests;

public class PayPeriodBuilder
{
    private int _payPeriodId;
    private int _assignmentId = 10;
    private int _dragonId = 20;
    private long _startDateUnix = 1 * Const.SECONDS_IN_A_DAY;
    private long _endDateUnix = 8 * Const.SECONDS_IN_A_DAY;
    private string _submissionStatus = "Draft";
    private List<HoursWorked> _hoursWorked = [];

    public PayPeriodBuilder WithPayPeriodId(int id) { _payPeriodId = id; return this; }
    public PayPeriodBuilder WithAssignmentId(int id) { _assignmentId = id; return this; }
    public PayPeriodBuilder WithDragonId(int id) { _dragonId = id; return this; }
    public PayPeriodBuilder WithStartDateUnix(long val) { _startDateUnix = val; return this; }
    public PayPeriodBuilder WithEndDateUnix(long val) { _endDateUnix = val; return this; }
    public PayPeriodBuilder WithSubmissionStatus(string val) { _submissionStatus = val; return this; }
    public PayPeriodBuilder WithHoursWorked(params HoursWorked[] items) { _hoursWorked = [..items]; return this; }
    public PayPeriodBuilder ClearHoursWorkedList()
    {
        _hoursWorked.Clear();
        return this;
    }
    public PayPeriodBuilder AddHoursWorked(long clockInSecondsRelativeToPeriodStart, long clockOutSecondsRelativeToPeriodStart)
    {
        _hoursWorked.Add(new HoursWorked
        {
           StartDateTimeUnix = _startDateUnix + clockInSecondsRelativeToPeriodStart,
           EndDateTimeUnix = _startDateUnix + clockOutSecondsRelativeToPeriodStart 
        });
        return this;
    }

    public PayPeriod Build() => new()
    {
        PayPeriodId = _payPeriodId,
        AssignmentId = _assignmentId,
        DragonId = _dragonId,
        StartDateUnix = _startDateUnix,
        EndDateUnix = _endDateUnix,
        SubmissionStatus = _submissionStatus,
        HoursWorked = _hoursWorked
    };
}
