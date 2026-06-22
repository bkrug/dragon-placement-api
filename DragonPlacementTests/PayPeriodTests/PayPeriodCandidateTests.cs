using System.Linq.Expressions;
using DragonPlacementApi;
using DragonPlacementApi.Endpoints;
using DragonPlacementApi.Extensions;
using DragonPlacementApi.Poco;
using Moq;
using Shouldly;
using TimekeepingDataLayer.Models;
using TimekeepingDataLayer.Repositories;

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
    private const long SECONDS_IN_A_WEEK = 7 * Const.SECONDS_IN_A_DAY;

    private static long GetMondayOfCurrentWeekUnix()
    {
        var today = DateTime.UtcNow.Date;
        var daysToSubtract = ((int)today.DayOfWeek + 6) % 7;
        var monday = today.AddDays(-daysToSubtract);
        return new DateTimeOffset(monday, TimeSpan.Zero).ToUnixTimeSeconds();
    }

    private static ValidPaySpan MakeExpectedValidPaySpan(long mondayUnix, int weeksAgo)
    {
        var start = DateTimeOffset.FromUnixTimeSeconds(mondayUnix - weeksAgo * SECONDS_IN_A_WEEK).UtcDateTime;
        return new ValidPaySpan
        {
            StartDate = start.ToIsoDateString(),
            EndDate = start.AddDays(6).ToIsoDateString()
        };
    }

    private static Mock<ITimekeepingUnitOfWork> MockWithExistingPayPeriods(IEnumerable<PayPeriod> existing)
    {
        var mock = new Mock<ITimekeepingUnitOfWork>();
        mock.Setup(u => u.PayPeriodRepository.Get(
                It.IsAny<Expression<Func<PayPeriod, bool>>>(), null, ""))
            .Returns(existing);
        return mock;
    }

    [Fact]
    public void GetValidPayPeriods_NoExistingPayPeriods_ExpectFourWeeklyCandidates()
    {
        var mondayUnix = GetMondayOfCurrentWeekUnix();
        var unitOfWorkMock = MockWithExistingPayPeriods([]);

        //Act
        var response = PayPeriodEndpoints.GetValidPayPeriods(unitOfWorkMock.Object, DRAGON_ID, ASSIGNMENT_ID);

        //Assert
        var payload = response.Value!.Payload;
        payload.ShouldBeEquivalentTo(new List<ValidPaySpan>
        {
            MakeExpectedValidPaySpan(mondayUnix, weeksAgo: 0),
            MakeExpectedValidPaySpan(mondayUnix, weeksAgo: 1),
            MakeExpectedValidPaySpan(mondayUnix, weeksAgo: 2),
            MakeExpectedValidPaySpan(mondayUnix, weeksAgo: 3)
        });
    }

    [Fact]
    public void GetValidPayPeriods_ExistingLastWeekAndTwoWeeksAgo_ExpectCurrentWeekAndThreeWeeksAgo()
    {
        var mondayUnix = GetMondayOfCurrentWeekUnix();
        var existing = new List<PayPeriod>
        {
            new PayPeriodBuilder()
                .WithPayPeriodId(101)
                .WithStartDateUnix(mondayUnix - 1 * SECONDS_IN_A_WEEK)
                .WithEndDateUnix(mondayUnix - 1 * SECONDS_IN_A_WEEK + 6 * Const.SECONDS_IN_A_DAY)
                .Build(),
            new PayPeriodBuilder()
                .WithPayPeriodId(102)
                .WithStartDateUnix(mondayUnix - 3 * SECONDS_IN_A_WEEK)
                .WithEndDateUnix(mondayUnix - 3 * SECONDS_IN_A_WEEK + 6 * Const.SECONDS_IN_A_DAY)
                .Build()
        };
        var unitOfWorkMock = MockWithExistingPayPeriods(existing);

        //Act
        var response = PayPeriodEndpoints.GetValidPayPeriods(unitOfWorkMock.Object, DRAGON_ID, ASSIGNMENT_ID);

        //Assert
        var payload = response.Value!.Payload;
        payload.ShouldBeEquivalentTo(new List<ValidPaySpan>
        {
            MakeExpectedValidPaySpan(mondayUnix, weeksAgo: 0),
            MakeExpectedValidPaySpan(mondayUnix, weeksAgo: 2)
        });
    }

    [Fact]
    public void GetValidPayPeriods_ExistingCurrentWeekAndLastWeek_ExpectTwoAndThreeWeeksAgo()
    {
        var mondayUnix = GetMondayOfCurrentWeekUnix();
        var existing = new List<PayPeriod>
        {
            new PayPeriodBuilder()
                .WithPayPeriodId(201)
                .WithStartDateUnix(mondayUnix)
                .WithEndDateUnix(mondayUnix + 6 * Const.SECONDS_IN_A_DAY)
                .Build(),
            new PayPeriodBuilder()
                .WithPayPeriodId(202)
                .WithStartDateUnix(mondayUnix - 1 * SECONDS_IN_A_WEEK)
                .WithEndDateUnix(mondayUnix - 1 * SECONDS_IN_A_WEEK + 6 * Const.SECONDS_IN_A_DAY)
                .Build()
        };
        var unitOfWorkMock = MockWithExistingPayPeriods(existing);

        //Act
        var response = PayPeriodEndpoints.GetValidPayPeriods(unitOfWorkMock.Object, DRAGON_ID, ASSIGNMENT_ID);

        //Assert
        var payload = response.Value!.Payload;
        payload.ShouldBeEquivalentTo(new List<ValidPaySpan>
        {
            MakeExpectedValidPaySpan(mondayUnix, weeksAgo: 2),
            MakeExpectedValidPaySpan(mondayUnix, weeksAgo: 3)
        });
    }
}
