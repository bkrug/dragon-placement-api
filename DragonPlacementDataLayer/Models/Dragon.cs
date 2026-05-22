using System;
using System.Collections.Generic;

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

    public virtual ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();

    public virtual ICollection<SkillTag> SkillTags { get; set; } = new List<SkillTag>();
}
