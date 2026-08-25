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
            [FromBody] CreateCustomerAndWorkRequest createCustomer
        )
    {
        return TypedResults.Ok(ValidatedResponse.Success);
    }

    public static async Task<Results<Ok<ValidatedResponse>, BadRequest<ValidatedResponse>>>
        CreateWorkRequetAsync(
            IBillingUnitOfWork unitOfWork,
            [FromQuery] int customerId,
            [FromBody] WorkRequestCreateEdit createEditWorkRequest
        )
    {
        return TypedResults.Ok(ValidatedResponse.Success);
    }

    public static async Task<Results<Ok<ValidatedResponse>, BadRequest<ValidatedResponse>>>
        EditWorkRequetAsync(
            IBillingUnitOfWork unitOfWork,
            [FromQuery] int customerId,
            [FromBody] WorkRequestCreateEdit createEditWorkRequest
        )
    {
        return TypedResults.Ok(ValidatedResponse.Success);
    }
}

/// <summary>
/// A new customer's first work request is created in the same call as the customer,
/// so this is a flat representation of the Customer and WorkRequest domain models combined.
/// </summary>
public class CreateCustomerAndWorkRequest
{
    public string CustomerName { get; set; } = null!;
    public string WorkRequestName { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string EstimatedStartDate { get; set; } = string.Empty;
    public string EstimatedEndDate { get; set; } = string.Empty;
    public int EstimatedWorkforceSize { get; set; }
}

public class WorkRequestCreateEdit
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string EstimatedStartDate { get; set; } = string.Empty;
    public string EstimatedEndDate { get; set; } = string.Empty;
    public int EstimatedWorkforceSize { get; set; }
}