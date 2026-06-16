namespace DragonPlacementDataLayer.Poco;

public class HoursWorkedWithJob
{
    public int HoursWorkedId { get; set; }
    public int AssignmentId { get; set; }
    public int DragonId { get; set; }
    public long StartDateTimeUnix { get; set; }
    public long EndDateTimeUnix { get; set; }
    public string JobTitle { get; set; } = null!;
    public string? EmployerName { get; set; }
}
