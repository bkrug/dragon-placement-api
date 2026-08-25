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
        CreateCustomerWithWorkRequestAsync(
            IBillingUnitOfWork unitOfWork,
            [FromBody] CreateCustomerAndWorkRequest createCustomer
        )
    {
        var result = await CustomerCreationService.CreateCustomerWithWorkRequest(unitOfWork, createCustomer).ConfigureAwait(false);
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
        CreateWorkRequestAsync(
            IBillingUnitOfWork unitOfWork,
            [FromQuery(Name = "customerId")] int customerId,
            [FromBody] WorkRequestCreateEdit createWorkRequest
        )
    {
        var result = await WorkRequestUpsertService.CreateWorkRequest(unitOfWork, createWorkRequest, customerId).ConfigureAwait(false);
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

    public static async Task<Results<Ok<ValidatedResponse>, NotFound<ValidatedResponse>, Conflict<ValidatedForm<ValidationFailures>>, BadRequest<ValidatedForm<ValidationFailures>>>>
        EditWorkRequestAsync(
            IBillingUnitOfWork unitOfWork,
            [FromQuery(Name = "workRequestId")] int workRequestId,
            [FromBody] WorkRequestCreateEdit editWorkRequest
        )
    {
        var result = await WorkRequestUpsertService.EditWorkRequest(unitOfWork, editWorkRequest, workRequestId).ConfigureAwait(false);
        if (result.IsFailure)
            return result.Error switch
            {
                WorkRequestNotFound => TypedResults.NotFound(ValidatedResponse.NotFound),
                WorkRequestNotInDraftStatus e => TypedResults.Conflict(new ValidatedForm<ValidationFailures>
                {
                    IsSuccess = false,
                    IsInternalError = false,
                    ValidationFailures = e.Failures
                }),
                WorkRequestEditInvalid e => TypedResults.BadRequest(new ValidatedForm<ValidationFailures>
                {
                    IsSuccess = false,
                    IsInternalError = false,
                    ValidationFailures = e.Failures
                }),
                _ => throw new InvalidOperationException()
            };
        return TypedResults.Ok(ValidatedResponse.Success);
    }
}