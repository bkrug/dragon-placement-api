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

public class CreateCustomerWithWorkRequetTests
{
    [Fact]
    public async Task CreateCustomerWithWorkRequet_ValidInput_ExpectCustomerInsertedWithOneWorkRequestAndSavesOnce()
    {
        var input = new CreateCustomerAndWorkRequest
        {
            CustomerName = "Acme Kingdom",
            WorkRequestName = "Castle Renovation",
            Description = "Reinforce the east wall",
            EstimatedStartDate = "1970-01-02",
            EstimatedEndDate = "1970-02-01",
            EstimatedWorkforceSize = 4
        };
        var expectedCustomer = new Customer
        {
            Name = "Acme Kingdom",
            WorkRequests =
            [
                new WorkRequest
                {
                    Name = "Castle Renovation",
                    Description = "Reinforce the east wall",
                    EstimatedStartDate = new DateTime(1970, 1, 2),
                    EstimatedEndDate = new DateTime(1970, 2, 1),
                    EstimatedWorkforceSize = 4
                }
            ]
        };
        var insertedCustomer = new Immutable<Customer>();
        var unitOfWorkMock = new Mock<IBillingUnitOfWork>();
        unitOfWorkMock.Setup(u => u.CustomerRepository.Insert(It.IsAny<Customer>()))
            .Callback<Customer>(insertedCustomer.Set);

        //Act
        var response = await WorkRequestEndpoints.CreateCustomerWithWorkRequetAsync(unitOfWorkMock.Object, input);

        //Assert
        response.Result.ShouldBeOfType<Ok<ValidatedResponse>>();
        insertedCustomer.Get().ShouldBeEquivalentTo(expectedCustomer);
        insertedCustomer.Get()!.WorkRequests.Count.ShouldBe(1);
        unitOfWorkMock.Verify(u => u.WorkRequestRepository.Insert(It.IsAny<WorkRequest>()), Times.Never);
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Theory]
    [InlineData("not-a-date",   "1970-02-01", 4,  "EstimatedStartDate",     "must be an ISO Date")]
    [InlineData("1970-01-02",   "not-a-date", 4,  "EstimatedEndDate",       "must be an ISO Date")]
    [InlineData("1970-01-02",   "1970-02-01", -7, "EstimatedWorkforceSize", "must be a non-negative number")]
    [InlineData("1970-02-01",   "1970-01-02", 4,  "EstimatedEndDate",       "must be later than start date")]
    public async Task CreateCustomerWithWorkRequet_InvalidInput_ExpectBadRequestAndDoesNotInsertOrSave(
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
        var response = await WorkRequestEndpoints.CreateCustomerWithWorkRequetAsync(unitOfWorkMock.Object, input);

        //Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<ValidationFailures>>>();
        var badResult = (BadRequest<ValidatedForm<ValidationFailures>>)response.Result;
        var failures = badResult.Value!.ValidationFailures.FieldFailures;
        failures[expectedFailureField].ShouldBe(expectedFailureMessage);
        unitOfWorkMock.Verify(u => u.CustomerRepository.Insert(It.IsAny<Customer>()), Times.Never);
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }
}
