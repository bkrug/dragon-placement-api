using DragonPlacementApi.Poco;

namespace DragonPlacementTests.PayPeriodTests;

public class PayPeriodCreateEditNewBuilder
{
    private int _assignmentId = 10;
    private int _dragonId = 20;
    private string _startDate = "1970-01-02";
    private string _endDate = "1970-01-09";
    private string _submissionStatus = "Draft";
    private List<HoursWorkedCreateEditNew> _hoursWorked =
    [
        new HoursWorkedCreateEditNew
        {
            StartDateTime = "1970-01-02T00:00:00",
            EndDateTime = "1970-01-02T01:00:00"
        }
    ];

    public PayPeriodCreateEditNewBuilder WithAssignmentId(int id) { _assignmentId = id; return this; }
    public PayPeriodCreateEditNewBuilder WithDragonId(int id) { _dragonId = id; return this; }
    public PayPeriodCreateEditNewBuilder WithStartDate(string val) { _startDate = val; return this; }
    public PayPeriodCreateEditNewBuilder WithEndDate(string val) { _endDate = val; return this; }
    public PayPeriodCreateEditNewBuilder WithSubmissionStatus(string val) { _submissionStatus = val; return this; }
    public PayPeriodCreateEditNewBuilder WithHoursWorked(params HoursWorkedCreateEditNew[] items) { _hoursWorked = [..items]; return this; }

    public PayPeriodCreateEditNew Build() => new()
    {
        AssignmentId = _assignmentId,
        DragonId = _dragonId,
        StartDate = _startDate,
        EndDate = _endDate,
        SubmissionStatus = _submissionStatus,
        HoursWorked = _hoursWorked
    };
}
