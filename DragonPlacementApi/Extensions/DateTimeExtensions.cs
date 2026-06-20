namespace DragonPlacementApi.Extensions;

//All Date, Time, and DateTime objects are currently unaware of timezones.
//This will probably be the case throughout this project for a long time.
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