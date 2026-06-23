using System;

namespace DragonAssignmentDomain.Poco;

public class JobWithCapacity
{
    public int JobId { get; set; }

    public string JobTitle { get; set; } = null!;

    public string? EmployerName { get; set; }

    public int FilledPositions { get; set; }

    public int NumberOfPositions { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }
}
