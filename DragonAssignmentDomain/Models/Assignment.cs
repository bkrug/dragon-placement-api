using System;
using System.Collections.Generic;

namespace DragonAssignmentDomain.Models;

public class Assignment
{
    public int AssignmentId { get; set; }

    public int DragonId { get; set; }

    public int JobId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public Dragon Dragon { get; set; } = null!;

    public Job Job { get; set; } = null!;
}
