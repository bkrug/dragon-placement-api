using DragonTimekeepingDomain.Models;

namespace DragonPlacementTests.PayPeriodTests;

public class PayPeriodBuilder
{
    private int _payPeriodId;
    private int _assignmentId = 10;
    private DateTime _startDate = new (1970, 1, 2);
    private DateTime _endDate = new (1970, 1, 9);
    private string _submissionStatus = "Draft";
    private List<HoursWorked> _hoursWorked = [];

    public PayPeriodBuilder WithPayPeriodId(int id) { _payPeriodId = id; return this; }
    public PayPeriodBuilder WithAssignmentId(int id) { _assignmentId = id; return this; }
    public PayPeriodBuilder WithStartDate(DateTime startDate) {
        _startDate = startDate;
        return this;
    }
    public PayPeriodBuilder WithEndDate(DateTime endDate) {
        _endDate = endDate;
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
            StartDateTime = _startDate.AddSeconds(clockInSeconds),
            EndDateTime = _startDate.AddSeconds(clockOutSeconds)
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
