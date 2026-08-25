using DragonBilling.Application;
using DragonBilling.Application.CustomerCreation;
using DragonBilling.Application.WorkRequestUpsert;
using DragonCommon.Domain.Poco;
using DragonPlacementApi.Poco;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace DragonPlacementApi.Endpoints;

public static class WorkRequestEndpoints
{
    public static async Task<Results<Ok<ValidatedResponse>, BadRequest<ValidatedForm<ValidationFailures>>>>
        CreateCustomerWithWorkRequetAsync(
            IBillingUnitOfWork unitOfWork,
            [FromBody] CreateCustomerAndWorkRequest createCustomer
        )
    {
        var result = await CustomerCreationService.CreateCustomerWithWorkRequest(createCustomer, unitOfWork).ConfigureAwait(false);
        return result.IsSuccess
            ? TypedResults.Ok(ValidatedResponse.Success)
            : TypedResults.BadRequest(new ValidatedForm<ValidationFailures>
            {
                IsSuccess = false,
                IsInternalError = false,
                ValidationFailures = result.Error
            });
    }

    public static async Task<Results<Ok<ValidatedResponse>, NotFound<ValidatedResponse>, BadRequest<ValidatedForm<ValidationFailures>>>>
        CreateWorkRequetAsync(
            IBillingUnitOfWork unitOfWork,
            [FromQuery(Name = "customerId")] int customerId,
            [FromBody] WorkRequestCreateEdit createWorkRequest
        )
    {
        var result = await WorkRequestUpsertService.CreateWorkRequest(createWorkRequest, customerId, unitOfWork).ConfigureAwait(false);
        if (result.IsFailure)
            return result.Error switch
            {
                CustomerNotFound => TypedResults.NotFound(ValidatedResponse.NotFound),
                WorkRequestInvalid e => TypedResults.BadRequest(new ValidatedForm<ValidationFailures>
                {
                    IsSuccess = false,
                    IsInternalError = false,
                    ValidationFailures = e.Failures
                }),
                _ => throw new InvalidOperationException()
            };
        return TypedResults.Ok(ValidatedResponse.Success);
    }

    public static async Task<Results<Ok<ValidatedResponse>, BadRequest<ValidatedResponse>>>
        EditWorkRequetAsync(
            IBillingUnitOfWork unitOfWork,
            [FromQuery(Name = "workRequestId")] int workRequestId,
            [FromBody] WorkRequestCreateEdit editWorkRequest
        )
    {
        return TypedResults.Ok(ValidatedResponse.Success);
    }
}