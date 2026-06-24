namespace DragonCommonApplication;

public static class DateTimeExtentions
{
    public static string ToIsoDateString(this DateTime dateTime)
    {
        return dateTime.ToString("yyyy-MM-dd");
    }

    public static string ToIsoDateTimeString(this DateTime dateTime)
    {
        return dateTime.ToString("yyyy-MM-ddTHH:mm:ss");
    }
}
