namespace DragonPlacementApi.Poco;

public class JobValidationFailures
{
    public string JobTitle { get; set; } = null!;
    public string NumberOfPositions { get; set; } = null!;
    public string StartDateUnix { get; set; } = null!;
    public string EndDateUnix { get; set; } = null!;
}

public class DragonValidationFailures
{
    public string GivenName { get; set; } = null!;
    public string WeightInKg { get; set; } = null!;
    public string LengthInMeters { get; set; } = null!;
    public string FightingSkills { get; set; } = null!;
}
