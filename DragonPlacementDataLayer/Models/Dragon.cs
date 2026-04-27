using System;
using System.Collections.Generic;

namespace DragonPlacementDataLayer.Models;

public partial class Dragon
{
    public int DragonId { get; set; }

    public string GivenName { get; set; } = null!;

    public string? FamilyName { get; set; }

    public int CanBreathFire { get; set; }

    public int CanTakePassengers { get; set; }

    public int? WeightInKg { get; set; }

    public int? LengthInMeters { get; set; }

    public string? FightingSkills { get; set; }
}
