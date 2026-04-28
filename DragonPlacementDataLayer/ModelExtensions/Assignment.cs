namespace DragonPlacementDataLayer.Models;

public partial class Assignment
{
    public DateTime StartDate => DateTimeOffset.FromUnixTimeSeconds(StartDateUnix).UtcDateTime;
    public DateTime GetStartDate() => DateTimeOffset.FromUnixTimeSeconds(StartDateUnix).UtcDateTime;
    public void SetStartDate(DateTime value)
    {
        StartDateUnix = new DateTimeOffset(value).ToUnixTimeSeconds();
    }

    public DateTime? EndDate => EndDateUnix == null ? null : DateTimeOffset.FromUnixTimeSeconds(EndDateUnix.Value).UtcDateTime;
    public DateTime? GetEndDate() => EndDateUnix == null ? null : DateTimeOffset.FromUnixTimeSeconds(EndDateUnix.Value).UtcDateTime;
    public void SetEndDate(DateTime? value)
    {
        EndDateUnix = value == null ? null : new DateTimeOffset(value.Value).ToUnixTimeSeconds();
    }
}