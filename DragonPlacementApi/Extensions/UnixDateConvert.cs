namespace DragonPlacementApi.Extensions;

//All Date, Time, and DateTime objects are currently unaware of timezones.
//This will probably be the case throughout this project for a long time.
public static class UnixDateConvert
{
    public static string ToIsoDate(long unixSeconds)
    {
        return DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime.ToString("yyyy-MM-dd");
    }

    public static string ToIsoDateTime(long unixSeconds)
    {
        return DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss");
    }

    public static string ToIsoTime(long unixSeconds)
    {
        return DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime.ToString("HH:mm:ss");
    }    
}
