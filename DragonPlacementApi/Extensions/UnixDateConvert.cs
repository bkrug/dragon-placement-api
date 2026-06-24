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

    public static long FromIsoDate(string isoDate)
    {
        return new DateTimeOffset(DateTime.Parse(isoDate), TimeSpan.Zero).ToUnixTimeSeconds();
    }

    public static long FromIsoDateTime(string isoDateTime)
    {
        return new DateTimeOffset(DateTime.Parse(isoDateTime), TimeSpan.Zero).ToUnixTimeSeconds();
    }

    public static DateTime ToDateTime(long unixSeconds)
    {
        return DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime;
    }

    public static long ToUnixSecond(DateTime dateTime)
    {
        return new DateTimeOffset(dateTime, TimeSpan.Zero).ToUnixTimeSeconds();
    }
}
