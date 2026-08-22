using DragonPlacementApi.Endpoints;
using DragonPlacementApi.Poco;
using DragonTimekeeping.Application;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using Shouldly;
using DragonTimekeeping.Domain.Enums;

namespace DragonUnitTests.PayPeriodTests;

public class PayPeriodWorkflowTests
{
    [Fact]
    public async Task SubmitPayPeriod_OriginallyInDraftStatus_ExpectSuccess()
    {
        const int PAY_PERIOD_ID = 14;
        var payPeriod = new PayPeriodBuilder()
            .WithPayPeriodId(PAY_PERIOD_ID)
            .WithSubmissionStatus(PayPeriodStatus.Draft)
            .Build();
        var unitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        unitOfWorkMock.Setup(u => u.PayPeriodRepository.GetByID(PAY_PERIOD_ID)).ReturnsAsync(payPeriod);

        //Act
        var response = await PayPeriodEndpoints.SubmitPayPeriodAsync(unitOfWorkMock.Object, PAY_PERIOD_ID);

        //Assert
        response.Result.ShouldBeOfType<Ok<ValidatedResponse>>();
        payPeriod.SubmissionStatus.ShouldBe(PayPeriodStatus.Submitted);
        unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Once);
    }

    [Theory]
    [InlineData(PayPeriodStatus.Submitted)]
    [InlineData(PayPeriodStatus.Billed)]
    public async Task SubmitPayPeriod_OriginallyNotDraftStatus_ExpectFailure(PayPeriodStatus existingStatus)
    {
        const int PAY_PERIOD_ID = 15;
        var payPeriod = new PayPeriodBuilder()
            .WithPayPeriodId(PAY_PERIOD_ID)
            .WithSubmissionStatus(existingStatus)
            .Build();
        var unitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        unitOfWorkMock.Setup(u => u.PayPeriodRepository.GetByID(PAY_PERIOD_ID)).ReturnsAsync(payPeriod);

        //Act
        var response = await PayPeriodEndpoints.SubmitPayPeriodAsync(unitOfWorkMock.Object, PAY_PERIOD_ID);

        //Assert
        response.Result.ShouldBeOfType<Conflict<ValidatedResponse>>();
        payPeriod.SubmissionStatus.ShouldBe(existingStatus);
        unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Never);
    }
}
