using System;

namespace DragonAssignmentDomain.Poco;

public class JobWithCapacity
{
    public int JobId { get; set; }

    public string JobTitle { get; set; } = null!;

    public string? EmployerName { get; set; }

    public int FilledPositions { get; set; }

    public int NumberOfPositions { get; set; }

    public string StartDate { get; set; } = null!;

    public string EndDate { get; set; } = null!;
}
