using System.Linq.Expressions;
using DragonBilling.Application;
using DragonBilling.Domain.Enum;
using DragonBilling.Domain.Models;
using DragonPlacementApi.Endpoints;
using DragonPlacementApi.Poco;
using DragonTimekeeping.Application;
using DragonTimekeeping.Domain.Enums;
using DragonTimekeeping.Domain.Models;
using DragonUnitTests.PayPeriodTests;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using Shouldly;

namespace DragonUnitTests.BillingTests;

public class BillableHoursCandidateTests
{
    private const int DRAFT_PAY_PERIOD_ID = 1;
    private const int BILLED_PAY_PERIOD_ID = 2;
    private const int SUBMITTED_PAY_PERIOD_ID_1 = 3;
    private const int SUBMITTED_PAY_PERIOD_ID_2 = 4;
    private const int SUBMITTED_PAY_PERIOD_ID_3 = 5;
    private const int SUBMITTED_PAY_PERIOD_ID_4 = 6;

    private const int DRAFT_ASSIGNMENT_ID = 101;
    private const int BILLED_ASSIGNMENT_ID = 102;
    private const int SUBMITTED_ASSIGNMENT_ID_1 = 103;
    private const int SUBMITTED_ASSIGNMENT_ID_2 = 104;
    private const int SUBMITTED_ASSIGNMENT_ID_3 = 105;
    private const int SUBMITTED_ASSIGNMENT_ID_4 = 106;

    private const int PAY_PERIOD_ID_FULLY_IN_RANGE = 7;
    private const int PAY_PERIOD_ID_END_DATE_IN_RANGE = 8;
    private const int PAY_PERIOD_ID_ENDS_BEFORE_RANGE = 9;
    private const int PAY_PERIOD_ID_ENDS_AFTER_RANGE = 10;

    private const int ASSIGNMENT_ID_FULLY_IN_RANGE = 107;
    private const int ASSIGNMENT_ID_END_DATE_IN_RANGE = 108;
    private const int ASSIGNMENT_ID_ENDS_BEFORE_RANGE = 109;
    private const int ASSIGNMENT_ID_ENDS_AFTER_RANGE = 110;

    private static readonly DateTime WEEK_START = new(2026, 8, 10); //Monday
    private static readonly DateTime WEEK_END = new(2026, 8, 16); //Sunday

    [Fact]
    public async Task BuildBillableHoursCandidates_MixOfPayPeriodStatuses_ExpectBillableHoursOnlyForSubmitted()
    {
        var payPeriods = new List<PayPeriod>
        {
            new PayPeriodBuilder()
                .WithPayPeriodId(DRAFT_PAY_PERIOD_ID)
                .WithAssignmentId(DRAFT_ASSIGNMENT_ID)
                .WithStartDate(WEEK_START)
                .WithEndDate(WEEK_END)
                .WithSubmissionStatus(PayPeriodStatus.Draft)
                .Build(),
            new PayPeriodBuilder()
                .WithPayPeriodId(BILLED_PAY_PERIOD_ID)
                .WithAssignmentId(BILLED_ASSIGNMENT_ID)
                .WithStartDate(WEEK_START)
                .WithEndDate(WEEK_END)
                .WithSubmissionStatus(PayPeriodStatus.Billed)
                .Build(),
            new PayPeriodBuilder()
                .WithPayPeriodId(SUBMITTED_PAY_PERIOD_ID_1)
                .WithAssignmentId(SUBMITTED_ASSIGNMENT_ID_1)
                .WithStartDate(WEEK_START)
                .WithEndDate(WEEK_END)
                .WithSubmissionStatus(PayPeriodStatus.Submitted)
                .Build(),
            new PayPeriodBuilder()
                .WithPayPeriodId(SUBMITTED_PAY_PERIOD_ID_2)
                .WithAssignmentId(SUBMITTED_ASSIGNMENT_ID_2)
                .WithStartDate(WEEK_START)
                .WithEndDate(WEEK_END)
                .WithSubmissionStatus(PayPeriodStatus.Submitted)
                .Build(),
        };
        var chargeRates = new List<ChargeRate>
        {
            new() { ChargeRateId = 501, AssignmentId = DRAFT_ASSIGNMENT_ID, HourlyRate = 50m },
            new() { ChargeRateId = 502, AssignmentId = BILLED_ASSIGNMENT_ID, HourlyRate = 55m },
            new() { ChargeRateId = 503, AssignmentId = SUBMITTED_ASSIGNMENT_ID_1, HourlyRate = 60m },
            new() { ChargeRateId = 504, AssignmentId = SUBMITTED_ASSIGNMENT_ID_2, HourlyRate = 65m },
        };

        var timekeepingUnitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        timekeepingUnitOfWorkMock.Setup(u => u.PayPeriodRepository.Get(
                It.IsAny<Expression<Func<PayPeriod, bool>>>(), null, ""))
            .Returns(payPeriods);

        var insertedBillableHours = new List<BillableHours>();
        var billingUnitOfWorkMock = new Mock<IBillingUnitOfWork>();
        billingUnitOfWorkMock.Setup(u => u.ChargeRateRepository.Get(
                It.IsAny<Expression<Func<ChargeRate, bool>>>(), null, ""))
            .Returns(chargeRates);
        billingUnitOfWorkMock.Setup(u => u.BillableHoursRepository.Insert(It.IsAny<BillableHours>()))
            .Callback<BillableHours>(insertedBillableHours.Add);

        //Act
        var response = await BillingEndpoints.BuildBillableHoursCandidatesAsync(
            billingUnitOfWorkMock.Object,
            timekeepingUnitOfWorkMock.Object,
            WEEK_START.ToString("yyyy-MM-dd"),
            WEEK_END.ToString("yyyy-MM-dd"));

        //Assert
        response.Result.ShouldBeOfType<Ok<ValidatedResponse>>();
        insertedBillableHours.Select(bh => bh.PayPeriodId).ToList().ShouldBeEquivalentTo(new List<int>
        {
            SUBMITTED_PAY_PERIOD_ID_1,
            SUBMITTED_PAY_PERIOD_ID_2
        });
        billingUnitOfWorkMock.Verify(u => u.SaveAsync(), Times.Once);
    }

    [Fact]
    public async Task BuildBillableHoursCandidates_SomePayPeriodsAlreadyHaveBillableHours_ExpectNoDuplicateBillableHours()
    {
        var payPeriods = new List<PayPeriod>
        {
            new PayPeriodBuilder()
                .WithPayPeriodId(SUBMITTED_PAY_PERIOD_ID_1)
                .WithAssignmentId(SUBMITTED_ASSIGNMENT_ID_1)
                .WithStartDate(WEEK_START)
                .WithEndDate(WEEK_END)
                .WithSubmissionStatus(PayPeriodStatus.Submitted)
                .Build(),
            new PayPeriodBuilder()
                .WithPayPeriodId(SUBMITTED_PAY_PERIOD_ID_2)
                .WithAssignmentId(SUBMITTED_ASSIGNMENT_ID_2)
                .WithStartDate(WEEK_START)
                .WithEndDate(WEEK_END)
                .WithSubmissionStatus(PayPeriodStatus.Submitted)
                .Build(),
            new PayPeriodBuilder()
                .WithPayPeriodId(SUBMITTED_PAY_PERIOD_ID_3)
                .WithAssignmentId(SUBMITTED_ASSIGNMENT_ID_3)
                .WithStartDate(WEEK_START)
                .WithEndDate(WEEK_END)
                .WithSubmissionStatus(PayPeriodStatus.Submitted)
                .Build(),
            new PayPeriodBuilder()
                .WithPayPeriodId(SUBMITTED_PAY_PERIOD_ID_4)
                .WithAssignmentId(SUBMITTED_ASSIGNMENT_ID_4)
                .WithStartDate(WEEK_START)
                .WithEndDate(WEEK_END)
                .WithSubmissionStatus(PayPeriodStatus.Submitted)
                .Build(),
        };
        var chargeRates = new List<ChargeRate>
        {
            new() { ChargeRateId = 503, AssignmentId = SUBMITTED_ASSIGNMENT_ID_1, HourlyRate = 60m },
            new() { ChargeRateId = 504, AssignmentId = SUBMITTED_ASSIGNMENT_ID_2, HourlyRate = 65m },
            new() { ChargeRateId = 505, AssignmentId = SUBMITTED_ASSIGNMENT_ID_3, HourlyRate = 70m },
            new() { ChargeRateId = 506, AssignmentId = SUBMITTED_ASSIGNMENT_ID_4, HourlyRate = 75m },
        };
        var existingBillableHours = new List<BillableHours>
        {
            new()
            {
                BillableHoursId = 901,
                ChargeRateId = 503,
                PayPeriodId = SUBMITTED_PAY_PERIOD_ID_1,
                HourlyRate = 60m,
                TotalHours = 0m,
                BillingStatus = BillingStatus.Draft
            },
            new()
            {
                BillableHoursId = 902,
                ChargeRateId = 504,
                PayPeriodId = SUBMITTED_PAY_PERIOD_ID_2,
                HourlyRate = 65m,
                TotalHours = 0m,
                BillingStatus = BillingStatus.Draft
            },
        };

        var timekeepingUnitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        timekeepingUnitOfWorkMock.Setup(u => u.PayPeriodRepository.Get(
                It.IsAny<Expression<Func<PayPeriod, bool>>>(), null, ""))
            .Returns(payPeriods);

        var insertedBillableHours = new List<BillableHours>();
        var billingUnitOfWorkMock = new Mock<IBillingUnitOfWork>();
        billingUnitOfWorkMock.Setup(u => u.ChargeRateRepository.Get(
                It.IsAny<Expression<Func<ChargeRate, bool>>>(), null, ""))
            .Returns(chargeRates);
        billingUnitOfWorkMock.Setup(u => u.BillableHoursRepository.Get(
                It.IsAny<Expression<Func<BillableHours, bool>>>(), null, ""))
            .Returns(existingBillableHours);
        billingUnitOfWorkMock.Setup(u => u.BillableHoursRepository.Insert(It.IsAny<BillableHours>()))
            .Callback<BillableHours>(insertedBillableHours.Add);

        //Act
        var response = await BillingEndpoints.BuildBillableHoursCandidatesAsync(
            billingUnitOfWorkMock.Object,
            timekeepingUnitOfWorkMock.Object,
            WEEK_START.ToString("yyyy-MM-dd"),
            WEEK_END.ToString("yyyy-MM-dd"));

        //Assert
        response.Result.ShouldBeOfType<Ok<ValidatedResponse>>();
        insertedBillableHours.Select(bh => bh.PayPeriodId).ToList().ShouldBeEquivalentTo(new List<int>
        {
            SUBMITTED_PAY_PERIOD_ID_3,
            SUBMITTED_PAY_PERIOD_ID_4
        });
        billingUnitOfWorkMock.Verify(u => u.SaveAsync(), Times.Once);
    }

    [Fact]
    public async Task BuildBillableHoursCandidates_PayPeriodsRelativeToDateRange_ExpectBillableHoursOnlyWhenEndDateInRange()
    {
        var payPeriods = new List<PayPeriod>
        {
            // A: starts and ends inside the queried range
            new PayPeriodBuilder()
                .WithPayPeriodId(PAY_PERIOD_ID_FULLY_IN_RANGE)
                .WithAssignmentId(ASSIGNMENT_ID_FULLY_IN_RANGE)
                .WithStartDate(WEEK_START)
                .WithEndDate(WEEK_END)
                .WithSubmissionStatus(PayPeriodStatus.Submitted)
                .Build(),
            // B: starts before the queried range, but ends inside it
            new PayPeriodBuilder()
                .WithPayPeriodId(PAY_PERIOD_ID_END_DATE_IN_RANGE)
                .WithAssignmentId(ASSIGNMENT_ID_END_DATE_IN_RANGE)
                .WithStartDate(WEEK_START.AddDays(-7))
                .WithEndDate(WEEK_END)
                .WithSubmissionStatus(PayPeriodStatus.Submitted)
                .Build(),
            // C: ends before the queried range starts
            new PayPeriodBuilder()
                .WithPayPeriodId(PAY_PERIOD_ID_ENDS_BEFORE_RANGE)
                .WithAssignmentId(ASSIGNMENT_ID_ENDS_BEFORE_RANGE)
                .WithStartDate(WEEK_START.AddDays(-14))
                .WithEndDate(WEEK_START.AddDays(-1))
                .WithSubmissionStatus(PayPeriodStatus.Submitted)
                .Build(),
            // D: ends after the queried range ends
            new PayPeriodBuilder()
                .WithPayPeriodId(PAY_PERIOD_ID_ENDS_AFTER_RANGE)
                .WithAssignmentId(ASSIGNMENT_ID_ENDS_AFTER_RANGE)
                .WithStartDate(WEEK_END.AddDays(1))
                .WithEndDate(WEEK_END.AddDays(7))
                .WithSubmissionStatus(PayPeriodStatus.Submitted)
                .Build(),
        };
        var chargeRates = new List<ChargeRate>
        {
            new() { ChargeRateId = 601, AssignmentId = ASSIGNMENT_ID_FULLY_IN_RANGE, HourlyRate = 60m },
            new() { ChargeRateId = 602, AssignmentId = ASSIGNMENT_ID_END_DATE_IN_RANGE, HourlyRate = 65m },
            new() { ChargeRateId = 603, AssignmentId = ASSIGNMENT_ID_ENDS_BEFORE_RANGE, HourlyRate = 70m },
            new() { ChargeRateId = 604, AssignmentId = ASSIGNMENT_ID_ENDS_AFTER_RANGE, HourlyRate = 75m },
        };

        var timekeepingUnitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        // Applies the real filter expression rather than ignoring it, since the date-range
        // filtering under test happens inside that expression, not anywhere downstream.
        timekeepingUnitOfWorkMock.Setup(u => u.PayPeriodRepository.Get(
                It.IsAny<Expression<Func<PayPeriod, bool>>>(), null, ""))
            .Returns((Expression<Func<PayPeriod, bool>> filter,
                    Func<IQueryable<PayPeriod>, IOrderedQueryable<PayPeriod>>? orderBy,
                    string includeProperties) =>
                payPeriods.AsQueryable().Where(filter).ToList());

        var insertedBillableHours = new List<BillableHours>();
        var billingUnitOfWorkMock = new Mock<IBillingUnitOfWork>();
        billingUnitOfWorkMock.Setup(u => u.ChargeRateRepository.Get(
                It.IsAny<Expression<Func<ChargeRate, bool>>>(), null, ""))
            .Returns(chargeRates);
        billingUnitOfWorkMock.Setup(u => u.BillableHoursRepository.Insert(It.IsAny<BillableHours>()))
            .Callback<BillableHours>(insertedBillableHours.Add);

        //Act
        var response = await BillingEndpoints.BuildBillableHoursCandidatesAsync(
            billingUnitOfWorkMock.Object,
            timekeepingUnitOfWorkMock.Object,
            WEEK_START.ToString("yyyy-MM-dd"),
            WEEK_END.ToString("yyyy-MM-dd"));

        //Assert
        response.Result.ShouldBeOfType<Ok<ValidatedResponse>>();
        insertedBillableHours.Select(bh => bh.PayPeriodId).ToList().ShouldBeEquivalentTo(new List<int>
        {
            PAY_PERIOD_ID_FULLY_IN_RANGE,
            PAY_PERIOD_ID_END_DATE_IN_RANGE
        });
        billingUnitOfWorkMock.Verify(u => u.SaveAsync(), Times.Once);
    }
}
