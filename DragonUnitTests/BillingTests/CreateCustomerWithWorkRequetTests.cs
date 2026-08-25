using DragonBilling.Application;
using DragonBilling.Domain.Models;
using DragonCommon.Application.Repositories;
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

    [Fact]
    public async Task CreateCustomerWithWorkRequet_StartDateAfterEndDate_ExpectBadRequestAndDoesNotInsertOrSave()
    {
        var input = new CreateCustomerAndWorkRequest
        {
            CustomerName = "Acme Kingdom",
            WorkRequestName = "Castle Renovation",
            Description = "Reinforce the east wall",
            EstimatedStartDate = "1970-02-01",
            EstimatedEndDate = "1970-01-02",
            EstimatedWorkforceSize = 4
        };
        var unitOfWorkMock = new Mock<IBillingUnitOfWork>();
        unitOfWorkMock.Setup(m => m.CustomerRepository).Returns(new Mock<IGenericRepository<Customer>>().Object);

        //Act
        var response = await WorkRequestEndpoints.CreateCustomerWithWorkRequetAsync(unitOfWorkMock.Object, input);

        //Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedResponse>>();
        var badResult = (BadRequest<ValidatedResponse>)response.Result;
        badResult.Value!.ValidationFailures.ShouldContain("Estimated start date must be before estimated end date.");
        unitOfWorkMock.Verify(u => u.CustomerRepository.Insert(It.IsAny<Customer>()), Times.Never);
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Theory]
    [InlineData("not-a-date", "EstimatedStartDate",     "Estimated start date must be an ISO date.")]
    [InlineData("not-a-date", "EstimatedEndDate",       "Estimated end date must be an ISO date.")]
    [InlineData(-7,           "EstimatedWorkforceSize", "Estimated workforce size must be a non-negative number.")]
    public async Task CreateCustomerWithWorkRequet_UnparsableField_ExpectBadRequestAndDoesNotInsertOrSave(
        object invalidValue,
        string invalidPropertyName,
        string expectedFailureMessage)
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
        typeof(CreateCustomerAndWorkRequest).GetProperty(invalidPropertyName)!.SetValue(input, invalidValue);
        var unitOfWorkMock = new Mock<IBillingUnitOfWork>();
        unitOfWorkMock.Setup(m => m.CustomerRepository).Returns(new Mock<IGenericRepository<Customer>>().Object);

        //Act
        var response = await WorkRequestEndpoints.CreateCustomerWithWorkRequetAsync(unitOfWorkMock.Object, input);

        //Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedResponse>>();
        var badResult = (BadRequest<ValidatedResponse>)response.Result;
        badResult.Value!.ValidationFailures.ShouldContain(expectedFailureMessage);
        unitOfWorkMock.Verify(u => u.CustomerRepository.Insert(It.IsAny<Customer>()), Times.Never);
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }
}
