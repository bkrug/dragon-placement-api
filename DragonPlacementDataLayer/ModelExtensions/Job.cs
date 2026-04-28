using System;
using System.Collections.Generic;

namespace DragonPlacementDataLayer.Models;

public partial class Job
{
    public DateTime GetStartDate() => DateTimeOffset.FromUnixTimeSeconds(StartDateUnix).UtcDateTime;
    public void SetStartDate(DateTime value)
    {
        StartDateUnix = new DateTimeOffset(value).ToUnixTimeSeconds();
    }

    public DateTime GetEndDate() => DateTimeOffset.FromUnixTimeSeconds(EndDateUnix).UtcDateTime;
    public void SetEndDate(DateTime value)
    {
        EndDateUnix = new DateTimeOffset(value).ToUnixTimeSeconds();
    }        
}
