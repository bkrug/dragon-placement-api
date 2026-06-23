using DragonPlacementApi.Endpoints;
using DragonPlacementApi.Extensions;
using DragonPlacementApi.Poco;
using Moq;
using Shouldly;
using DragonTimekeepingDomain.Models;
using DragonTimekeepingApplication;

namespace DragonPlacementTests.PayPeriodTests;

/// <summary>
/// GetValidPayPeriodsOld() is depricated.
/// We will eventually delete it.
/// Use GetValidPayPeriodsNew() in the future.
/// </summary>
public class PayPeriodCandidateTests
{
    private const int DRAGON_ID = 20;
    private const int ASSIGNMENT_ID = 10;

    private static DateTime GetMondayOfCurrentWeek()
    {
        var today = DateTime.UtcNow.Date;
        var daysToSubtract = ((int)today.DayOfWeek + 6) % 7;
        return today.AddDays(-daysToSubtract);
    }

    private static ValidPaySpan MakeExpectedValidPaySpan(DateTime monday, int weeksAgo)
    {
        var start = monday.AddDays(-weeksAgo * 7);
        return new ValidPaySpan
        {
            StartDate = start.ToIsoDateString(),
            EndDate = start.AddDays(6).ToIsoDateString()
        };
    }

    private static Mock<ITimekeepingUnitOfWork> MockWithExistingPayPeriods(IEnumerable<PayPeriod> existing)
    {
        var mock = new Mock<ITimekeepingUnitOfWork>();
        mock.Setup(u => u.GetPayPeriodsByAssignment(ASSIGNMENT_ID))
            .Returns(existing);
        return mock;
    }

    [Fact]
    public void GetValidPayPeriods_NoExistingPayPeriods_ExpectFourWeeklyCandidates()
    {
        var monday = GetMondayOfCurrentWeek();
        var unitOfWorkMock = MockWithExistingPayPeriods([]);

        //Act
        var response = PayPeriodEndpoints.GetValidPayPeriods(unitOfWorkMock.Object, DRAGON_ID, ASSIGNMENT_ID);

        //Assert
        var payload = response.Value!.Payload;
        payload.ShouldBeEquivalentTo(new List<ValidPaySpan>
        {
            MakeExpectedValidPaySpan(monday, weeksAgo: 0),
            MakeExpectedValidPaySpan(monday, weeksAgo: 1),
            MakeExpectedValidPaySpan(monday, weeksAgo: 2),
            MakeExpectedValidPaySpan(monday, weeksAgo: 3)
        });
    }

    [Fact]
    public void GetValidPayPeriods_ExistingLastWeekAndTwoWeeksAgo_ExpectCurrentWeekAndThreeWeeksAgo()
    {
        var monday = GetMondayOfCurrentWeek();
        var existing = new List<PayPeriod>
        {
            new PayPeriodBuilder()
                .WithPayPeriodId(101)
                .WithStartDate(monday.AddDays(-7))
                .WithEndDate(monday.AddDays(-1))
                .Build(),
            new PayPeriodBuilder()
                .WithPayPeriodId(102)
                .WithStartDate(monday.AddDays(-21))
                .WithEndDate(monday.AddDays(-15))
                .Build()
        };
        var unitOfWorkMock = MockWithExistingPayPeriods(existing);

        //Act
        var response = PayPeriodEndpoints.GetValidPayPeriods(unitOfWorkMock.Object, DRAGON_ID, ASSIGNMENT_ID);

        //Assert
        var payload = response.Value!.Payload;
        payload.ShouldBeEquivalentTo(new List<ValidPaySpan>
        {
            MakeExpectedValidPaySpan(monday, weeksAgo: 0),
            MakeExpectedValidPaySpan(monday, weeksAgo: 2)
        });
    }

    [Fact]
    public void GetValidPayPeriods_ExistingCurrentWeekAndLastWeek_ExpectTwoAndThreeWeeksAgo()
    {
        var monday = GetMondayOfCurrentWeek();
        var existing = new List<PayPeriod>
        {
            new PayPeriodBuilder()
                .WithPayPeriodId(201)
                .WithStartDate(monday)
                .WithEndDate(monday.AddDays(6))
                .Build(),
            new PayPeriodBuilder()
                .WithPayPeriodId(202)
                .WithStartDate(monday.AddDays(-7))
                .WithEndDate(monday.AddDays(-1))
                .Build()
        };
        var unitOfWorkMock = MockWithExistingPayPeriods(existing);

        //Act
        var response = PayPeriodEndpoints.GetValidPayPeriods(unitOfWorkMock.Object, DRAGON_ID, ASSIGNMENT_ID);

        //Assert
        var payload = response.Value!.Payload;
        payload.ShouldBeEquivalentTo(new List<ValidPaySpan>
        {
            MakeExpectedValidPaySpan(monday, weeksAgo: 2),
            MakeExpectedValidPaySpan(monday, weeksAgo: 3)
        });
    }
}
