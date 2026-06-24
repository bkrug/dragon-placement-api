using System;
using System.Collections.Generic;

namespace DragonAssignmentDomain.Models;

public partial class Job
{
    //public DateTime StartDate => DateTimeOffset.FromUnixTimeSeconds(StartDateUnix).UtcDateTime;
    public DateTime GetStartDate() => StartDate;
    public void SetStartDate(DateTime value)
    {
        StartDate = value;
    }

    //public DateTime EndDate => DateTimeOffset.FromUnixTimeSeconds(EndDateUnix).UtcDateTime;
    public DateTime GetEndDate() => EndDate;
    public void SetEndDate(DateTime value)
    {
        EndDate = value;
    }
}
