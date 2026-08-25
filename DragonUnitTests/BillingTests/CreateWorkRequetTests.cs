using DragonBilling.Application;
using DragonBilling.Application.WorkRequestUpsert;
using DragonBilling.Domain.Models;
using DragonCommon.Application.Repositories;
using DragonCommon.Domain.Poco;
using DragonPlacementApi.Endpoints;
using DragonPlacementApi.Poco;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using Shouldly;

namespace DragonUnitTests.BillingTests;

public class CreateWorkRequetTests
{
    private const int CUSTOMER_ID = 55;

    [Fact]
    public async Task CreateWorkRequet_CustomerExists_ExpectWorkRequestInsertedAndSavesOnce()
    {
        // The customer already has one work request on file; this call adds a second one.
        // CreateWorkRequetAsync only checks existence by id, so the existing WorkRequest
        // itself isn't referenced by the mock setup below.
        var input = new WorkRequestCreateEdit
        {
            Name = "Moat Excavation",
            Description = "Dig a defensive moat",
            EstimatedStartDate = "1970-03-01",
            EstimatedEndDate = "1970-04-01",
            EstimatedWorkforceSize = 6
        };
        var expectedWorkRequest = new WorkRequest
        {
            CustomerId = CUSTOMER_ID,
            Name = "Moat Excavation",
            Description = "Dig a defensive moat",
            EstimatedStartDate = new DateTime(1970, 3, 1),
            EstimatedEndDate = new DateTime(1970, 4, 1),
            EstimatedWorkforceSize = 6
        };
        var insertedWorkRequest = new Immutable<WorkRequest>();
        var unitOfWorkMock = new Mock<IBillingUnitOfWork>();
        unitOfWorkMock.Setup(u => u.CustomerExists(CUSTOMER_ID)).ReturnsAsync(true);
        unitOfWorkMock.Setup(u => u.WorkRequestRepository.Insert(It.IsAny<WorkRequest>()))
            .Callback<WorkRequest>(insertedWorkRequest.Set);

        //Act
        var response = await WorkRequestEndpoints.CreateWorkRequetAsync(unitOfWorkMock.Object, CUSTOMER_ID, input);

        //Assert
        response.Result.ShouldBeOfType<Ok<ValidatedResponse>>();
        insertedWorkRequest.Get().ShouldBeEquivalentTo(expectedWorkRequest);
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateWorkRequet_CustomerDoesNotExist_ExpectNotFoundAndDoesNotInsertOrSave()
    {
        var input = new WorkRequestCreateEdit
        {
            Name = "Moat Excavation",
            Description = "Dig a defensive moat",
            EstimatedStartDate = "1970-03-01",
            EstimatedEndDate = "1970-04-01",
            EstimatedWorkforceSize = 6
        };
        var unitOfWorkMock = new Mock<IBillingUnitOfWork>();
        unitOfWorkMock.Setup(u => u.CustomerExists(CUSTOMER_ID)).ReturnsAsync(false);
        unitOfWorkMock.Setup(m => m.WorkRequestRepository).Returns(new Mock<IGenericRepository<WorkRequest>>().Object);

        //Act
        var response = await WorkRequestEndpoints.CreateWorkRequetAsync(unitOfWorkMock.Object, CUSTOMER_ID, input);

        //Assert
        response.Result.ShouldBeOfType<NotFound<ValidatedResponse>>();
        unitOfWorkMock.Verify(u => u.WorkRequestRepository.Insert(It.IsAny<WorkRequest>()), Times.Never);
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Theory]
    [InlineData("not-a-date", "1970-02-01", 4,  "EstimatedStartDate",     "must be an ISO Date")]
    [InlineData("1970-01-02", "not-a-date", 4,  "EstimatedEndDate",       "must be an ISO Date")]
    [InlineData("1970-01-02", "1970-02-01", -7, "EstimatedWorkforceSize", "must be a non-negative number")]
    [InlineData("1970-02-01", "1970-01-02", 4,  "EstimatedStartDate",     "start date must preceed end date")]
    public async Task CreateWorkRequet_InvalidInput_ExpectBadRequestAndDoesNotInsertOrSave(
        string estimatedStartDate,
        string estimatedEndDate,
        int estimatedWorkforceSize,
        string expectedFailureField,
        string expectedFailureMessage)
    {
        var input = new WorkRequestCreateEdit
        {
            Name = "Moat Excavation",
            Description = "Dig a defensive moat",
            EstimatedStartDate = estimatedStartDate,
            EstimatedEndDate = estimatedEndDate,
            EstimatedWorkforceSize = estimatedWorkforceSize
        };
        var unitOfWorkMock = new Mock<IBillingUnitOfWork>();
        unitOfWorkMock.Setup(u => u.CustomerExists(CUSTOMER_ID)).ReturnsAsync(true);
        unitOfWorkMock.Setup(m => m.WorkRequestRepository).Returns(new Mock<IGenericRepository<WorkRequest>>().Object);

        //Act
        var response = await WorkRequestEndpoints.CreateWorkRequetAsync(unitOfWorkMock.Object, CUSTOMER_ID, input);

        //Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<ValidationFailures>>>();
        var badResult = (BadRequest<ValidatedForm<ValidationFailures>>)response.Result;
        var failures = badResult.Value!.ValidationFailures.FieldFailures;
        failures[expectedFailureField].ShouldBe(expectedFailureMessage);
        unitOfWorkMock.Verify(u => u.WorkRequestRepository.Insert(It.IsAny<WorkRequest>()), Times.Never);
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }
}
