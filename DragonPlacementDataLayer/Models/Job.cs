using System;
using System.Collections.Generic;

namespace DragonPlacementDataLayer.Models;

public partial class Job
{
    public int JobId { get; set; }

    public string JobTitle { get; set; } = null!;

    public string? EmployerName { get; set; }

    public int NumberOfPositions { get; set; }

    public int StartDate { get; set; }

    public int EndDate { get; set; }

    public virtual ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
}
