using System;
using System.Collections.Generic;

namespace DragonAssignmentDomain.Models;

public partial class Job
{
    public int JobId { get; set; }

    public string JobTitle { get; set; } = null!;

    public string? EmployerName { get; set; }

    public int NumberOfPositions { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public virtual ICollection<Assignment> Assignments { get; set; } = [];

    public virtual ICollection<SkillTag> SkillTags { get; set; } = [];

    public Assignment Assign(int dragonId)
    {
        var assignment = new Assignment
        {
            DragonId = dragonId,
            JobId = JobId,
            StartDate = StartDate,
            EndDate = EndDate
        };
        Assignments.Add(assignment);
        return assignment;
    }
}
