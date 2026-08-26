using DragonBilling.Application;
using DragonBilling.Application.CustomerCreation;
using DragonBilling.Domain.Models;
using DragonCommon.Application.Repositories;
using DragonCommon.Domain.Poco;
using DragonPlacementApi.Endpoints;
using DragonPlacementApi.Poco;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using Shouldly;

namespace DragonUnitTests.BillingTests;

public class CreateCustomerWithWorkRequestTests
{
    [Fact]
    public async Task CreateCustomerWithWorkRequests_ValidInput_ExpectCustomerInsertedWithOneWorkRequestAndSavesOnce()
    {
        var input = new CreateCustomerAndWorkRequest
        {
            CustomerName = "Acme Kingdom",
            WorkRequestName = "Castle Renovation",
            Description = "Reinforce the east wall",
            EstimatedStartDate = "2026-01-02",
            EstimatedEndDate = "2026-02-01",
            EstimatedWorkforceSize = 4
        };
        var expectedCustomer = new Customer { Name = "Acme Kingdom" };
        var expectedWorkRequest = new WorkRequest
        {
            Name = "Castle Renovation",
            Description = "Reinforce the east wall",
            EstimatedStartDate = new DateTime(2026, 1, 2),
            EstimatedEndDate = new DateTime(2026, 2, 1),
            EstimatedWorkforceSize = 4,
            Customer = expectedCustomer
        };
        expectedCustomer.WorkRequests = [expectedWorkRequest];
        var insertedCustomer = new Immutable<Customer>();
        var unitOfWorkMock = new Mock<IBillingUnitOfWork>();
        unitOfWorkMock.Setup(u => u.CustomerRepository.Insert(It.IsAny<Customer>()))
            .Callback<Customer>(insertedCustomer.Set);

        //Act
        var response = await WorkRequestEndpoints.CreateCustomerWithWorkRequestAsync(unitOfWorkMock.Object, input);

        //Assert
        response.Result.ShouldBeOfType<Ok<ValidatedPayload<WorkRequest>>>();
        var okResult = (Ok<ValidatedPayload<WorkRequest>>)response.Result;
        okResult.Value!.Payload.ShouldBeEquivalentTo(expectedWorkRequest);
        insertedCustomer.Get()!.WorkRequests.Count.ShouldBe(1);
        insertedCustomer.Get()!.WorkRequests.Single().ShouldBeSameAs(okResult.Value!.Payload);
        unitOfWorkMock.Verify(u => u.WorkRequestRepository.Insert(It.IsAny<WorkRequest>()), Times.Never);
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Theory]
    [InlineData("not-a-date",   "2026-02-01", 4,  "EstimatedStartDate",     "must be an ISO Date")]
    [InlineData("2026-01-02",   "not-a-date", 4,  "EstimatedEndDate",       "must be an ISO Date")]
    [InlineData("2026-01-02",   "2026-02-01", -7, "EstimatedWorkforceSize", "must be a non-negative number")]
    [InlineData("2026-02-01",   "2026-01-02", 4,  "EstimatedStartDate",     "start date must preceed end date")]
    public async Task CreateCustomerWithWorkRequests_InvalidInput_ExpectBadRequestAndDoesNotInsertOrSave(
        string estimatedStartDate,
        string estimatedEndDate,
        int estimatedWorkforceSize,
        string expectedFailureField,
        string expectedFailureMessage)
    {
        var input = new CreateCustomerAndWorkRequest
        {
            CustomerName = "Acme Kingdom",
            WorkRequestName = "Castle Renovation",
            Description = "Reinforce the east wall",
            EstimatedStartDate = estimatedStartDate,
            EstimatedEndDate = estimatedEndDate,
            EstimatedWorkforceSize = estimatedWorkforceSize
        };
        var unitOfWorkMock = new Mock<IBillingUnitOfWork>();
        unitOfWorkMock.Setup(m => m.CustomerRepository).Returns(new Mock<IGenericRepository<Customer>>().Object);

        //Act
        var response = await WorkRequestEndpoints.CreateCustomerWithWorkRequestAsync(unitOfWorkMock.Object, input);

        //Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<ValidationFailures>>>();
        var badResult = (BadRequest<ValidatedForm<ValidationFailures>>)response.Result;
        var failures = badResult.Value!.ValidationFailures.FieldFailures;
        failures[expectedFailureField].ShouldBe(expectedFailureMessage);
        unitOfWorkMock.Verify(u => u.CustomerRepository.Insert(It.IsAny<Customer>()), Times.Never);
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }
}
