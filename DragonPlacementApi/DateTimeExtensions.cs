namespace DragonPlacementApi;

public static class DateTimeExtentions
{
    public static string ToIsoDateString(this DateTime dateTime)
    {
        return dateTime.ToString("yyyy-MM-dd");
    }
}