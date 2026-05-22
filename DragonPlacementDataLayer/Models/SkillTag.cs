using System;
using System.Collections.Generic;

namespace DragonPlacementDataLayer.Models;

public partial class SkillTag
{
    public int SkillTagId { get; set; }

    public string SkillName { get; set; } = null!;

    public virtual ICollection<Dragon> Dragons { get; set; } = [];

    public virtual ICollection<Job> Jobs { get; set; } = [];
}
