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