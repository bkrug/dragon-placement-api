namespace DragonPlacementDataLayer.Models;

public partial class Assignment
{
    public DateTime GetStartDate() => DateTimeOffset.FromUnixTimeSeconds(StartDateUnix).UtcDateTime;
    public void SetStartDate(DateTime value)
    {
        StartDateUnix = new DateTimeOffset(value).ToUnixTimeSeconds();
    }

    public DateTime? GetEndDate() => EndDateUnix == null ? null : DateTimeOffset.FromUnixTimeSeconds(EndDateUnix.Value).UtcDateTime;
    public void SetEndDate(DateTime? value)
    {
        EndDateUnix = value == null ? null : new DateTimeOffset(value.Value).ToUnixTimeSeconds();
    }
}