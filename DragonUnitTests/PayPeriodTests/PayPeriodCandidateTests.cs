using System.Linq.Expressions;
using DragonPlacementApi.Endpoints;
using DragonCommonApplication;
using DragonPlacementApi.Poco;
using Moq;
using Shouldly;
using DragonTimekeeping.Domain.Models;
using DragonTimekeeping.Application;
using DragonTimekeeping.Application.PotentialPayPeriodQuery;

namespace DragonUnitTests.PayPeriodTests;

public class PayPeriodCandidateTests
{
    private const int DRAGON_ID = 20;
    private const int ASSIGNMENT_ID = 10;

    private static DateTime GetMondayOfCurrentWeek()
    {
        var today = DateTime.UtcNow.Date;
        var daysToSubtract = ((int)today.DayOfWeek + 6) % 7;
        var monday = today.AddDays(-daysToSubtract);
        return monday;
    }    

    private static ValidPaySpan GetPaySpan(int weeksAgo)
    {
        var monday = GetMondayOfCurrentWeek();
        var sunday = monday.AddDays(6);
        return new ValidPaySpan
        {
            StartDate = monday.AddDays(-weeksAgo * 7).ToIsoDateString(),
            EndDate = sunday.AddDays(-weeksAgo * 7).ToIsoDateString()
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
        var unitOfWorkMock = MockWithExistingPayPeriods([]);

        //Act
        var response = PayPeriodEndpoints.GetValidPayPeriods(unitOfWorkMock.Object, DRAGON_ID, ASSIGNMENT_ID);

        //Assert
        var payload = response.Value!.Payload;
        payload.ShouldBeEquivalentTo(new List<ValidPaySpan>
        {
            GetPaySpan(weeksAgo: 0),
            GetPaySpan(weeksAgo: 1),
            GetPaySpan(weeksAgo: 2),
            GetPaySpan(weeksAgo: 3)
        });
    }

    [Fact]
    public void GetValidPayPeriods_ExistingLastWeekAndTwoWeeksAgo_ExpectCurrentWeekAndThreeWeeksAgo()
    {
        var monday = GetMondayOfCurrentWeek();
        var sunday = monday.AddDays(6);
        var existing = new List<PayPeriod>
        {
            new PayPeriodBuilder()
                .WithPayPeriodId(101)
                .WithStartDate(monday.AddDays(-7))
                .WithEndDate(sunday.AddDays(-7))
                .Build(),
            new PayPeriodBuilder()
                .WithPayPeriodId(102)
                .WithStartDate(monday.AddDays(-21))
                .WithEndDate(sunday.AddDays(-21))
                .Build()
        };
        var unitOfWorkMock = MockWithExistingPayPeriods(existing);

        //Act
        var response = PayPeriodEndpoints.GetValidPayPeriods(unitOfWorkMock.Object, DRAGON_ID, ASSIGNMENT_ID);

        //Assert
        var payload = response.Value!.Payload;
        payload.ShouldBeEquivalentTo(new List<ValidPaySpan>
        {
            GetPaySpan(weeksAgo: 0),
            GetPaySpan(weeksAgo: 2)
        });
    }

    [Fact]
    public void GetValidPayPeriods_ExistingCurrentWeekAndLastWeek_ExpectTwoAndThreeWeeksAgo()
    {
        var monday = GetMondayOfCurrentWeek();
        var sunday = monday.AddDays(6);
        var existing = new List<PayPeriod>
        {
            new PayPeriodBuilder()
                .WithPayPeriodId(201)
                .WithStartDate(monday)
                .WithEndDate(sunday)
                .Build(),
            new PayPeriodBuilder()
                .WithPayPeriodId(202)
                .WithStartDate(monday.AddDays(-7))
                .WithEndDate(sunday.AddDays(-7))
                .Build()
        };
        var unitOfWorkMock = MockWithExistingPayPeriods(existing);

        //Act
        var response = PayPeriodEndpoints.GetValidPayPeriods(unitOfWorkMock.Object, DRAGON_ID, ASSIGNMENT_ID);

        //Assert
        var payload = response.Value!.Payload;
        payload.ShouldBeEquivalentTo(new List<ValidPaySpan>
        {
            GetPaySpan(weeksAgo: 2),
            GetPaySpan(weeksAgo: 3)
        });
    }
}
