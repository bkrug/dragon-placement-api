using CSharpFunctionalExtensions;
using DragonBilling.Domain.Models;
using DragonCommon.Domain.Poco;

namespace DragonBilling.Application.CustomerCreation;

public static class CustomerCreationService
{
    public static async Task<Result<Customer, ValidationFailures>> CreateCustomerWithWorkRequest(
        CreateCustomerAndWorkRequest input, IBillingUnitOfWork unitOfWork)
    {
        var result = CreateCustomerAndWorkRequestMapper.ToCustomer(input)
            .Bind(customer => customer.WorkRequests.Single().Validate().Map(_ => customer));

        if (result.IsSuccess)
        {
            unitOfWork.CustomerRepository.Insert(result.Value);
            await unitOfWork.SaveChangesAsync().ConfigureAwait(false);
        }

        return result;
    }
}
