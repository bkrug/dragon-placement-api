using DragonCommon.Application.Repositories;
using DragonPlacementApi.Endpoints;
using DragonPlacementApi.Poco;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using Shouldly;
using DragonTimekeeping.Application;
using DragonTimekeeping.Domain.Enums;

namespace DragonUnitTests.PayPeriodTests;

public class PayPeriodDeleteTests
{
    [Theory]
    [InlineData(PayPeriodStatus.Draft)]
    [InlineData(PayPeriodStatus.Submitted)]
    public async Task DeletePayPeriod_AcceptableStatus_ExpectOkAndSavesOnce(PayPeriodStatus existingStatus)
    {
        const int PAY_PERIOD_ID = 42;
        var payPeriod = new PayPeriodBuilder()
            .WithPayPeriodId(PAY_PERIOD_ID)
            .WithSubmissionStatus(existingStatus)
            .Build();
        var unitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        unitOfWorkMock.Setup(u => u.PayPeriodRepository.GetByID(PAY_PERIOD_ID)).ReturnsAsync(payPeriod);
        unitOfWorkMock.Setup(u => u.PayPeriodRepository.Delete(PAY_PERIOD_ID)).Returns(DeleteResult.Deleted);

        //Act
        var response = await PayPeriodEndpoints.DeletePayPeriodAsync(unitOfWorkMock.Object, PAY_PERIOD_ID);

        //Assert
        response.Result.ShouldBeOfType<Ok<ValidatedResponse>>();
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeletePayPeriod_BilledStatus_ExpectConflictAndDoesNotSave()
    {
        const int PAY_PERIOD_ID = 43;
        var payPeriod = new PayPeriodBuilder()
            .WithPayPeriodId(PAY_PERIOD_ID)
            .WithSubmissionStatus(PayPeriodStatus.Billed)
            .Build();
        var unitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        unitOfWorkMock.Setup(u => u.PayPeriodRepository.GetByID(PAY_PERIOD_ID)).ReturnsAsync(payPeriod);

        //Act
        var response = await PayPeriodEndpoints.DeletePayPeriodAsync(unitOfWorkMock.Object, PAY_PERIOD_ID);

        //Assert
        response.Result.ShouldBeOfType<Conflict<ValidatedResponse>>();
        unitOfWorkMock.Verify(u => u.PayPeriodRepository.Delete(It.IsAny<int>()), Times.Never);
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeletePayPeriod_NotFound_ExpectNotFoundAndDoesNotSave()
    {
        const int PAY_PERIOD_ID = 999;
        var unitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        unitOfWorkMock.Setup(u => u.PayPeriodRepository.Delete(PAY_PERIOD_ID)).Returns(DeleteResult.NotFound);

        //Act
        var response = await PayPeriodEndpoints.DeletePayPeriodAsync(unitOfWorkMock.Object, PAY_PERIOD_ID);

        //Assert
        response.Result.ShouldBeOfType<NotFound<ValidatedResponse>>();
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }    
}
