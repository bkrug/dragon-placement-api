namespace DragonAssignmentApplication.JobUpsert;

public class JobCreateEdit
{
    public string JobTitle { get; set; } = null!;
    public string? EmployerName { get; set; }
    public int NumberOfPositions { get; set; }
    public long StartDateUnix { get; set; }
    public long EndDateUnix { get; set; }
    public IList<int> SkillTagIds { get; set; } = [];
}
