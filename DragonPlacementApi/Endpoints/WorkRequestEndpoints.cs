using DragonBilling.Application;
using DragonPlacementApi.Poco;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace DragonPlacementApi.Endpoints;

public static class WorkRequestEndpoints
{
    public static async Task<Results<Ok<ValidatedResponse>, BadRequest<ValidatedResponse>>>
        CreateCutomerWithWorkRequetAsync(
            IBillingUnitOfWork unitOfWork,
            object createCustomer
        )
    {
        return TypedResults.Ok(ValidatedResponse.Success);
    }

    public static async Task<Results<Ok<ValidatedResponse>, BadRequest<ValidatedResponse>>>
        CreateWorkRequetAsync(
            IBillingUnitOfWork unitOfWork,
            [FromQuery] int customerId,
            [FromBody] object createEditWorkRequest
        )
    {
        return TypedResults.Ok(ValidatedResponse.Success);
    } 

    public static async Task<Results<Ok<ValidatedResponse>, BadRequest<ValidatedResponse>>>
        EditWorkRequetAsync(
            IBillingUnitOfWork unitOfWork,
            [FromQuery] int customerId,
            [FromBody] object createEditWorkRequest
        )
    {
        return TypedResults.Ok(ValidatedResponse.Success);
    }        
}

