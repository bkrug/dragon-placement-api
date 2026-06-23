using System;
using System.Collections.Generic;

namespace DragonAssignmentDomain.Models;

public class Job
{
    public int JobId { get; set; }

    public string JobTitle { get; set; } = null!;

    public string? EmployerName { get; set; }

    public int NumberOfPositions { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public ICollection<Assignment> Assignments { get; set; } = [];

    public ICollection<SkillTag> SkillTags { get; set; } = [];
}
