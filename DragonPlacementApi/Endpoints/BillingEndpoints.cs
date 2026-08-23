using CSharpFunctionalExtensions;
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
        var workflowResult = await PayPeriodBillingQuerier
            .GetSubmittedPayPeriodsForBilling(timekeepingUnitOfWork, startDateString, endDateString)
            .Tap(submittedPayPeriods => BillableHoursGenerationService.GenerateBillableHoursAsync(unitOfWork, submittedPayPeriods));
        return workflowResult.IsSuccess
            ? TypedResults.Ok(ValidatedResponse.Success)
            : TypedResults.BadRequest(new ValidatedResponse { ValidationFailures = [workflowResult.Error] });
    }
}