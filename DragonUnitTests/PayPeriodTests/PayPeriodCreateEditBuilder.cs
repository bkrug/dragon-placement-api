using DragonPlacementApi.Poco;
using DragonTimekeepingApplication.Dto;

namespace DragonUnitTests.PayPeriodTests;

public class PayPeriodCreateEditBuilder
{
    private int _assignmentId = 10;
    private int _dragonId = 20;
    private string _startDate = "1970-01-02";
    private string _endDate = "1970-01-09";
    private string _submissionStatus = "Draft";
    private List<HoursWorkedCreateEdit> _hoursWorked = [];

    public PayPeriodCreateEditBuilder WithAssignmentId(int id) { _assignmentId = id; return this; }
    public PayPeriodCreateEditBuilder WithDragonId(int id) { _dragonId = id; return this; }
    public PayPeriodCreateEditBuilder WithStartDate(string val) { _startDate = val; return this; }
    public PayPeriodCreateEditBuilder WithEndDate(string val) { _endDate = val; return this; }
    public PayPeriodCreateEditBuilder WithSubmissionStatus(string val) { _submissionStatus = val; return this; }
    public PayPeriodCreateEditBuilder AddHoursWorked(string clockInTime, string clockOutTime)
    {
        _hoursWorked.Add(new HoursWorkedCreateEdit
        {
            StartDateTime = clockInTime,
            EndDateTime = clockOutTime
        });
        return this;
    }

    public PayPeriodCreateEdit Build() => new()
    {
        AssignmentId = _assignmentId,
        StartDate = _startDate,
        EndDate = _endDate,
        SubmissionStatus = _submissionStatus,
        HoursWorked = _hoursWorked
    };
}
