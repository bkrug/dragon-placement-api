namespace DragonAssignment.Application.JobUpsert;

public class JobCreateEdit
{
    public string JobTitle { get; set; } = null!;
    public string? EmployerName { get; set; }
    public int NumberOfPositions { get; set; }
    public string StartDate { get; set; } = null!;
    public string EndDate { get; set; } = null!;
    public IList<int> SkillTagIds { get; set; } = [];
}
