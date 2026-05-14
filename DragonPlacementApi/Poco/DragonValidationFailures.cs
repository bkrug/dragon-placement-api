using System;

namespace DragonPlacementApi.Poco;

public class DragonValidationFailures
{
    public string GivenName { get; set; } = null!;
    public string CanBreathFire { get; set; } = null!;
    public string CanTakePassengers { get; set; } = null!;
    public string WeightInKg { get; set; } = null!;
    public string LengthInMeters { get; set; } = null!;
    public string FightingSkills { get; set; } = null!;
}
