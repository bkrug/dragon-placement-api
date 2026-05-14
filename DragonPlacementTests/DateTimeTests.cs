using DragonPlacementDataLayer.Models;
using Shouldly;

namespace DragonPlacementTests;

/// <summary>
/// These tests make certain that our code can avoid the 2038 problem.
/// If Unix epoc times are stored as 32-bit signed integers, some dates will be outside of the range of what is possible.
/// If an iteration of this test ever fails, we probably are representing a date as "int" instead of "long".
/// </summary>
public class DateTimeTests
{
    private static DateTime RoundTripStartDate<T>(DateTime targetDate) where T : new()
    {
        var obj = new T();
        typeof(T).GetMethod("SetStartDate")!.Invoke(obj, [targetDate]);
        return (DateTime)typeof(T).GetMethod("GetStartDate")!.Invoke(obj, null)!;
    }

    private static DateTime RoundTripEndDate<T>(DateTime targetDate) where T : new()
    {
        var obj = new T();
        typeof(T).GetMethod("SetEndDate")!.Invoke(obj, [targetDate]);
        return (DateTime)typeof(T).GetMethod("GetEndDate")!.Invoke(obj, null)!;
    }

    [Theory]
    [InlineData(1901, 2, 1)]     //This date would not be possible with 32-bit signed integer
    [InlineData(1969, 12, 15)]
    [InlineData(1970, 1, 15)]
    [InlineData(2038, 12, 25)]   //This date would not be possible with 32-bit signed integer
    [InlineData(2106, 3, 5)]     //This date would not be possible with 32-bit unsigned integer
    public void Assignment_SetStartDate_ReturnSameDate(int year, int month, int date)
    {
        var targetDate = new DateTime(year, month, date, 0, 0, 0, DateTimeKind.Utc);
        RoundTripStartDate<Assignment>(targetDate).ShouldBe(targetDate, TimeSpan.FromSeconds(1));
        RoundTripEndDate<Assignment>(targetDate).ShouldBe(targetDate, TimeSpan.FromSeconds(1));
        RoundTripStartDate<Job>(targetDate).ShouldBe(targetDate, TimeSpan.FromSeconds(1));
        RoundTripEndDate<Job>(targetDate).ShouldBe(targetDate, TimeSpan.FromSeconds(1));
    }
}