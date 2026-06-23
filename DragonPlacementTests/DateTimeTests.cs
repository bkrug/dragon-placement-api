using DragonAssignmentDomain.Models;
using Shouldly;

namespace DragonPlacementTests;

/// <summary>
/// These tests verify that DateTime properties on domain models
/// can store and retrieve dates correctly, including dates that would
/// have been problematic with 32-bit unix timestamps.
/// </summary>
public class DateTimeTests
{
    [Theory]
    [InlineData(1901, 2, 1)]     //This date would not be possible with 32-bit signed integer
    [InlineData(1969, 12, 15)]
    [InlineData(1970, 1, 15)]
    [InlineData(2038, 12, 25)]   //This date would not be possible with 32-bit signed integer
    [InlineData(2106, 3, 5)]     //This date would not be possible with 32-bit unsigned integer
    public void Assignment_StartDateEndDate_RoundTripsCorrectly(int year, int month, int date)
    {
        var targetDate = new DateTime(year, month, date, 0, 0, 0, DateTimeKind.Utc);

        var assignment = new Assignment { StartDate = targetDate, EndDate = targetDate };
        assignment.StartDate.ShouldBe(targetDate);
        assignment.EndDate.ShouldBe(targetDate);

        var job = new Job { StartDate = targetDate, EndDate = targetDate };
        job.StartDate.ShouldBe(targetDate);
        job.EndDate.ShouldBe(targetDate);
    }
}
