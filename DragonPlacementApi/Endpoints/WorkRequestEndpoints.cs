using DragonBilling.Application;
using DragonBilling.Application.CustomerCreation;
using DragonBilling.Application.WorkRequestUpsert;
using DragonBilling.Domain.Models;
using DragonCommon.Domain.Poco;
using DragonPlacementApi.Poco;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace DragonPlacementApi.Endpoints;

public static class WorkRequestEndpoints
{
    public static PagedData<WorkRequest> GetWorkRequests(
            IBillingUnitOfWork unitOfWork,
            [FromQuery(Name = "offset")] int offset = 0,
            [FromQuery(Name = "limit")] int limit = 20)
    {
        var results = unitOfWork.WorkRequestRepository
            .Get(
                orderBy: q => q.OrderBy(wr => wr.Customer.Name).ThenBy(wr => wr.EstimatedStartDate),
                includeProperties: nameof(WorkRequest.Customer)
            );
        return new()
        {
            Offset = offset,
            Limit = limit,
            TotalRecords = results.Count(),
            Data = results.Skip(offset).Take(limit).ToList()
        };
    }

    public static Ok<ValidatedPayload<List<Customer>>> SearchCustomersByName(
            IBillingUnitOfWork unitOfWork,
            [FromQuery(Name = "name")] string name,
            [FromQuery(Name = "count")] int count)
    {
        var allNames = string.IsNullOrWhiteSpace(name);
        var customerList = unitOfWork.CustomerRepository
            .Get(
                filter: c => allNames || c.Name.Contains(name),
                orderBy: q => q.OrderByDescending(c => c.CustomerId)
            )
            .Take(count)
            .ToList();
        return TypedResults.Ok(ValidatedPayload<List<Customer>>.FromPayload(customerList));
    }

    public static Results<Ok<ValidatedPayload<WorkRequest>>, NotFound<ValidatedResponse>>
        GetWorkRequest(
            IBillingUnitOfWork unitOfWork,
            [FromRoute(Name = "workRequestId")] int workRequestId)
    {
        var workRequest = unitOfWork.WorkRequestRepository
            .Get(filter: wr => wr.WorkRequestId == workRequestId, includeProperties: nameof(WorkRequest.Customer))
            .FirstOrDefault();
        return workRequest == null
            ? TypedResults.NotFound(ValidatedResponse.NotFound)
            : TypedResults.Ok(ValidatedPayload<WorkRequest>.FromPayload(workRequest));
    }
    
    public static async Task<Results<Ok<ValidatedPayload<WorkRequest>>, BadRequest<ValidatedForm<ValidationFailures>>>>
        CreateCustomerWithWorkRequestAsync(
            IBillingUnitOfWork unitOfWork,
            [FromBody] CreateCustomerAndWorkRequest createCustomer
        )
    {
        var result = await CustomerCreationService.CreateCustomerWithWorkRequest(unitOfWork, createCustomer).ConfigureAwait(false);
        return result.IsSuccess
            ? TypedResults.Ok(ValidatedPayload<WorkRequest>.FromPayload(result.Value))
            : TypedResults.BadRequest(new ValidatedForm<ValidationFailures>
            {
                IsSuccess = false,
                IsInternalError = false,
                ValidationFailures = result.Error
            });
    }

    public static async Task<Results<Ok<ValidatedPayload<WorkRequest>>, NotFound<ValidatedResponse>, BadRequest<ValidatedForm<ValidationFailures>>>>
        CreateWorkRequestAsync(
            IBillingUnitOfWork unitOfWork,
            [FromRoute(Name = "customerId")] int customerId,
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
        return TypedResults.Ok(ValidatedPayload<WorkRequest>.FromPayload(result.Value));
    }

    public static async Task<Results<Ok<ValidatedPayload<WorkRequest>>, NotFound<ValidatedResponse>, Conflict<ValidatedForm<ValidationFailures>>, BadRequest<ValidatedForm<ValidationFailures>>>>
        EditWorkRequestAsync(
            IBillingUnitOfWork unitOfWork,
            [FromRoute(Name = "workRequestId")] int workRequestId,
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
        return TypedResults.Ok(ValidatedPayload<WorkRequest>.FromPayload(result.Value));
    }
}