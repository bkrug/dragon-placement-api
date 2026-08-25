using CSharpFunctionalExtensions;
using DragonBilling.Domain.Models;
using DragonCommon.Domain.Poco;

namespace DragonBilling.Application.CustomerCreation;

public static class CustomerCreationService
{
    public static Task<Result<Customer, ValidationFailures>> CreateCustomerWithWorkRequest(
        CreateCustomerAndWorkRequest input, IBillingUnitOfWork unitOfWork)
    {
        return CreateCustomerAndWorkRequestMapper.ToCustomer(input)
            .Bind(customer => customer.WorkRequests.Single().Validate().Map(_ => customer))
            .Tap(customer => unitOfWork.CustomerRepository.Insert(customer))
            .Tap(async _ => await unitOfWork.SaveChangesAsync().ConfigureAwait(false));
    }
}
