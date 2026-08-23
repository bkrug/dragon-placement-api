using DragonBilling.Application;
using DragonBilling.Application.BillableHoursGeneration;
using DragonPlacementApi.Poco;
using DragonTimekeeping.Application;
using DragonTimekeeping.Application.PayPeriodBillingQuery;
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
        var payPeriodsResult = PayPeriodBillingQuerier.GetSubmittedPayPeriodsForBilling(
            timekeepingUnitOfWork, startDateString, endDateString);
        if (payPeriodsResult.IsFailure)
            return TypedResults.BadRequest(new ValidatedResponse { ValidationFailures = [payPeriodsResult.Error] });
        var submittedPayPeriods = payPeriodsResult.Value;

        await BillableHoursGenerationService.GenerateBillableHoursAsync(unitOfWork, submittedPayPeriods);

        return TypedResults.Ok(ValidatedResponse.Success);
    }
}