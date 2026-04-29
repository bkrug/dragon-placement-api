using System;

namespace DragonPlacementDataLayer.Poco;

public class JobWithCapacity
{
    public int JobId { get; set; }

    public string JobTitle { get; set; } = null!;

    public string? EmployerName { get; set; }

    public int FilledPositions { get; set; }

    public int NumberOfPositions { get; set; }

    public long StartDateUnix { get; set; }

    public long EndDateUnix { get; set; }
}