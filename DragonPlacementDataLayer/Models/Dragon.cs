using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DragonPlacementDataLayer.Models;

public partial class Dragon
{
    public int DragonId { get; set; }

    public string GivenName { get; set; } = null!;

    public string? FamilyName { get; set; }

    public bool CanBreathFire { get; set; }

    public bool CanTakePassengers { get; set; }

    public int? WeightInKg { get; set; }

    public int? LengthInMeters { get; set; }

    public string? FightingSkills { get; set; }

    public virtual ICollection<Assignment> Assignments { get; set; } = [];
}
