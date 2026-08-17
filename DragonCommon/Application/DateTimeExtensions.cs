namespace DragonCommon.Application;

public static class DateTimeExtentions
{
    public static string ToIsoDateString(this DateTime dateTime)
    {
        return dateTime.ToString(Const.ISO_DATE);
    }

    public static string ToIsoDateTimeString(this DateTime dateTime)
    {
        return dateTime.ToString(Const.ISO_DATETIME);
    }
}
