namespace DragonPlacementApi.Poco;

public class DragonCreateEdit
{
    public string GivenName { get; set; } = null!;
    public string? FamilyName { get; set; }
    public int? WeightInKg { get; set; }
    public int? LengthInMeters { get; set; }
    public string? FightingSkills { get; set; }
    public IList<int> SkillTagIds { get; set; } = [];
}

public class JobCreateEdit
{
    public string JobTitle { get; set; } = null!;
    public string? EmployerName { get; set; }
    public int NumberOfPositions { get; set; }
    public long StartDateUnix { get; set; }
    public long EndDateUnix { get; set; }
    public IList<int> SkillTagIds { get; set; } = [];
}

public class HoursWorkedCreateEdit
{
    public int AssignmentId { get; set; }
    public int DragonId { get; set; }
    public long StartDateTimeUnix { get; set; }
    public long EndDateTimeUnix { get; set; }
}