using System;
using System.Collections.Generic;

namespace DragonAssignmentDomain.Models;

public class SkillTag
{
    public int SkillTagId { get; set; }

    public string SkillName { get; set; } = null!;

    public ICollection<Dragon> Dragons { get; set; } = [];

    public ICollection<Job> Jobs { get; set; } = [];
}
