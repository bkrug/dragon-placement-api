using System;
using System.Collections.Generic;

namespace DragonPlacementDataLayer.Models;

public partial class Assignment
{
    public int AssignmentId { get; set; }

    public int DragonId { get; set; }

    public int JobId { get; set; }

    public long StartDateUnix { get; set; }

    public long? EndDateUnix { get; set; }

    public virtual Dragon Dragon { get; set; } = null!;

    public virtual Job Job { get; set; } = null!;
}
