using System;
using System.Collections.Generic;

namespace DragonPlacementDataLayer.Models;

public partial class Assignment
{
    public int AssignmentId { get; set; }

    public int DragonId { get; set; }

    public int JobId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public virtual Dragon Dragon { get; set; } = null!;

    public virtual Job Job { get; set; } = null!;
}
