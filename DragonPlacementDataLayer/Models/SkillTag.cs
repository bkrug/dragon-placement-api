using System;
using System.Collections.Generic;

namespace DragonPlacementDataLayer.Models;

public partial class SkillTag
{
    public int SkillTagId { get; set; }

    public string SkillName { get; set; } = null!;

    public virtual ICollection<Dragon> Dragons { get; set; } = new List<Dragon>();

    public virtual ICollection<Job> Jobs { get; set; } = new List<Job>();
}
