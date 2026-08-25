using DragonBilling.Application;
using DragonBilling.Application.WorkRequestUpsert;
using DragonBilling.Domain.Enum;
using DragonBilling.Domain.Models;
using DragonCommon.Domain.Poco;
using DragonPlacementApi.Endpoints;
using DragonPlacementApi.Poco;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using Shouldly;

namespace DragonUnitTests.BillingTests;

public class EditWorkRequestTests
{
    private const int CUSTOMER_ID = 55;
    private const int WORK_REQUEST_ID = 77;

    [Fact]
    public async Task EditWorkRequests_WorkRequestExists_ExpectWorkRequestUpdatedAndSavesOnce()
    {
        var existingWorkRequest = new WorkRequest
        {
            WorkRequestId = WORK_REQUEST_ID,
            CustomerId = CUSTOMER_ID,
            WorkRequestStatus = WorkRequestStatus.Draft,
            Name = "Moat Excavation",
            Description = "Dig a defensive moat",
            EstimatedStartDate = new DateTime(2026, 1, 1),
            EstimatedEndDate = new DateTime(2026, 2, 1),
            EstimatedWorkforceSize = 3
        };
        var input = new WorkRequestCreateEdit
        {
            Name = "Moat Excavation and Drawbridge",
            Description = "Dig a defensive moat and install a drawbridge",
            EstimatedStartDate = "2026-03-01",
            EstimatedEndDate = "2026-04-01",
            EstimatedWorkforceSize = 6
        };
        var expectedWorkRequest = new WorkRequest
        {
            WorkRequestId = WORK_REQUEST_ID,
            CustomerId = CUSTOMER_ID,
            WorkRequestStatus = WorkRequestStatus.Draft,
            Name = "Moat Excavation and Drawbridge",
            Description = "Dig a defensive moat and install a drawbridge",
            EstimatedStartDate = new DateTime(2026, 3, 1),
            EstimatedEndDate = new DateTime(2026, 4, 1),
            EstimatedWorkforceSize = 6
        };
        var unitOfWorkMock = new Mock<IBillingUnitOfWork>();
        unitOfWorkMock.Setup(u => u.WorkRequestRepository.GetByID(WORK_REQUEST_ID)).ReturnsAsync(existingWorkRequest);

        //Act
        var response = await WorkRequestEndpoints.EditWorkRequestAsync(unitOfWorkMock.Object, WORK_REQUEST_ID, input);

        //Assert
        response.Result.ShouldBeOfType<Ok<ValidatedResponse>>();
        existingWorkRequest.ShouldBeEquivalentTo(expectedWorkRequest);
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Theory]
    [InlineData("not-a-date", "2026-02-01", 4,  "EstimatedStartDate",     "must be an ISO Date")]
    [InlineData("2026-01-02", "not-a-date", 4,  "EstimatedEndDate",       "must be an ISO Date")]
    [InlineData("2026-01-02", "2026-02-01", -7, "EstimatedWorkforceSize", "must be a non-negative number")]
    [InlineData("2026-02-01", "2026-01-02", 4,  "EstimatedStartDate",     "start date must preceed end date")]
    public async Task EditWorkRequests_InvalidInput_ExpectBadRequestAndDoesNotSave(
        string estimatedStartDate,
        string estimatedEndDate,
        int estimatedWorkforceSize,
        string expectedFailureField,
        string expectedFailureMessage)
    {
        var existingWorkRequest = new WorkRequest
        {
            WorkRequestId = WORK_REQUEST_ID,
            CustomerId = CUSTOMER_ID,
            WorkRequestStatus = WorkRequestStatus.Draft,
            Name = "Moat Excavation",
            Description = "Dig a defensive moat",
            EstimatedStartDate = new DateTime(2026, 1, 1),
            EstimatedEndDate = new DateTime(2026, 2, 1),
            EstimatedWorkforceSize = 3
        };
        var input = new WorkRequestCreateEdit
        {
            Name = "Moat Excavation",
            Description = "Dig a defensive moat",
            EstimatedStartDate = estimatedStartDate,
            EstimatedEndDate = estimatedEndDate,
            EstimatedWorkforceSize = estimatedWorkforceSize
        };
        var unitOfWorkMock = new Mock<IBillingUnitOfWork>();
        unitOfWorkMock.Setup(u => u.WorkRequestRepository.GetByID(WORK_REQUEST_ID)).ReturnsAsync(existingWorkRequest);

        //Act
        var response = await WorkRequestEndpoints.EditWorkRequestAsync(unitOfWorkMock.Object, WORK_REQUEST_ID, input);

        //Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<ValidationFailures>>>();
        var badResult = (BadRequest<ValidatedForm<ValidationFailures>>)response.Result;
        var failures = badResult.Value!.ValidationFailures.FieldFailures;
        failures[expectedFailureField].ShouldBe(expectedFailureMessage);
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Theory]
    [InlineData(WorkRequestStatus.Approved)]
    [InlineData(WorkRequestStatus.Completed)]
    public async Task EditWorkRequests_WorkRequestNotInDraftStatus_ExpectConflictAndDoesNotSave(
        WorkRequestStatus workRequestStatus)
    {
        var existingWorkRequest = new WorkRequest
        {
            WorkRequestId = WORK_REQUEST_ID,
            CustomerId = CUSTOMER_ID,
            WorkRequestStatus = workRequestStatus,
            Name = "Moat Excavation",
            Description = "Dig a defensive moat",
            EstimatedStartDate = new DateTime(2026, 1, 1),
            EstimatedEndDate = new DateTime(2026, 2, 1),
            EstimatedWorkforceSize = 3
        };
        var input = new WorkRequestCreateEdit
        {
            Name = "Moat Excavation and Drawbridge",
            Description = "Dig a defensive moat and install a drawbridge",
            EstimatedStartDate = "2026-03-01",
            EstimatedEndDate = "2026-04-01",
            EstimatedWorkforceSize = 6
        };
        var unitOfWorkMock = new Mock<IBillingUnitOfWork>();
        unitOfWorkMock.Setup(u => u.WorkRequestRepository.GetByID(WORK_REQUEST_ID)).ReturnsAsync(existingWorkRequest);

        //Act
        var response = await WorkRequestEndpoints.EditWorkRequestAsync(unitOfWorkMock.Object, WORK_REQUEST_ID, input);

        //Assert
        response.Result.ShouldBeOfType<Conflict<ValidatedResponse>>();
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }
}
