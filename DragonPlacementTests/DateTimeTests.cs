using DragonPlacementDataLayer.Models;
using Shouldly;

namespace DragonPlacementTests;

/// <summary>
/// These tests make certain that our code can avoid the 2038 problem.
/// If Unix epoc times are stored as 32-bit signed integers, some dates will be outside of the range of what is possible.
/// </summary>
public class DateTimeTests
{
    delegate DateTime SetAndGetDate(DateTime targetDate);

    private DateTime AssignmentStartDate(DateTime targetDate)
    {
        var assignment = new Assignment();
        assignment.SetStartDate(targetDate);
        return assignment.GetStartDate();
    }

    private DateTime AssignmentEndDate(DateTime targetDate)
    {
        var assignment = new Assignment();
        assignment.SetEndDate(targetDate);
        return assignment.GetEndDate()!.Value;
    }

    private DateTime JobStartDate(DateTime targetDate)
    {
        var assignment = new Job();
        assignment.SetStartDate(targetDate);
        return assignment.GetStartDate();
    }

    private DateTime JobEndDate(DateTime targetDate)
    {
        var assignment = new Job();
        assignment.SetEndDate(targetDate);
        return assignment.GetEndDate();
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
        var delegates = new List<SetAndGetDate>()
        {
            AssignmentStartDate,
            AssignmentEndDate,
            JobStartDate,
            JobEndDate
        };
        for (int i = 0; i < delegates.Count; ++i)
        {
            var testDelagate = delegates[i];
            var name = testDelagate.GetType().Name;
            var calculatedDate = testDelagate(targetDate);
            calculatedDate.ShouldBe(targetDate, TimeSpan.FromSeconds(1), $"Failure happend running delegate at index {i}.");
        }
    }
}