using DragonBilling.Application;
using DragonPlacementApi.Poco;
using DragonTimekeeping.Application;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace DragonPlacementApi.Endpoints;

public class BillingEndpoints
{
    public static async Task<Results<Ok<ValidatedResponse>, BadRequest<ValidatedResponse>>>
        BuildBillableHoursCandidatesAsync(
            IBillingUnitOfWork unitOfWork,
            ITimekeepingUnitOfWork timekeepingUnitOfWork,
            [FromQuery(Name = "startDate")] string startDateString,
            [FromQuery(Name = "endDate")] string endDateString
        )
    {
        return TypedResults.Ok(ValidatedResponse.Success);
    }
}