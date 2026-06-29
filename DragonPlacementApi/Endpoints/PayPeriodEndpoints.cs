using DragonAssignmentApplication;
using DragonCommonApplication.Repositories;
using DragonPlacementApi.Poco;
using DragonTimekeepingApplication;
using DragonTimekeepingApplication.PayPeriodUpsert;
using DragonTimekeepingApplication.PotentialPayPeriodQuery;
using DragonTimekeepingApplication.SinglePayPeriodQuery;
using DragonTimekeepingDomain.Models;
using DragonCommonDomain.Poco;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace DragonPlacementApi.Endpoints;

public class PayPeriodEndpoints
{
    public static PagedData<PayPeriod> GetPayPeriods(
            ITimekeepingUnitOfWork unitOfWork,
            [FromRoute(Name = "unusedId")] int unusedId,
            [FromRoute(Name = "assignmentId")] int assignmentId,
            [FromQuery(Name = "offset")] int offset = 0,
            [FromQuery(Name = "limit")] int limit = 20)
    {
        var results = unitOfWork.PayPeriodRepository
            .Get(
                filter: pp => pp.AssignmentId == assignmentId,
                orderBy: q => q.OrderByDescending(pp => pp.StartDate)
            );
        return new()
        {
            Offset = offset,
            Limit = limit,
            TotalRecords = results.Count(),
            Data = results.Skip(offset).Take(limit).ToList()
        };
    }

    public static Ok<ValidatedPayload<List<ValidPaySpan>>> GetValidPayPeriods(
            ITimekeepingUnitOfWork unitOfWork,
            [FromRoute(Name = "unusedId")] int unusedId,
            [FromRoute(Name = "assignmentId")] int assignmentId)
    {
        var candidates = PotentialPayPeriodQuerier.GetValidPayPeriods(unitOfWork, assignmentId);
        return TypedResults.Ok(ValidatedPayload<List<ValidPaySpan>>.FromPayload(candidates));
    }

    public static async Task<Results<Ok<ValidatedPayload<PayPeriodView>>, NotFound<ValidatedResponse>>>
        GetPayPeriodAsync(
            ITimekeepingUnitOfWork unitOfWork,
            IDragonPlacementUnitOfWork assignmentUnitOfWork,
            [FromRoute(Name = "payPeriodId")] int payPeriodId)
    {
        var entry = await unitOfWork.GetPayPeriodWithHoursWorkedAsync(payPeriodId).ConfigureAwait(false);
        if (entry == null)
            return TypedResults.NotFound(ValidatedResponse.NotFound);

        var assignment = await assignmentUnitOfWork.GetAssignmentWithDragonAndJobAsync(entry.AssignmentId).ConfigureAwait(false);

        var view = PayPeriodViewMapper.ToView(
            entry,
            dragonName: $"{assignment?.Dragon.GivenName} {assignment?.Dragon.FamilyName}",
            assignmentDescription: $"{assignment?.Job.JobTitle} at {assignment?.Job.EmployerName}");
        return TypedResults.Ok(ValidatedPayload<PayPeriodView>.FromPayload(view));
    }

    public static async Task<Results<Ok<ValidatedPayload<PayPeriod>>, BadRequest<ValidatedForm<ValidationFailures>>>>
        CreatePayPeriodAsync(
            ITimekeepingUnitOfWork unitOfWork,
            [FromBody] PayPeriodCreateEdit input)
    {
        var creationResult = await PayPeriodUpsertService.CreatePayPeriodAsync(unitOfWork, input);
        if (creationResult.IsSuccess)
            return TypedResults.Ok(ValidatedPayload<PayPeriod>.FromPayload(creationResult.Value));
        else
            return TypedResults.BadRequest(new ValidatedForm<ValidationFailures>
            {
                IsSuccess = false,
                IsInternalError = false,
                ValidationFailures = creationResult.Error
            });
    }

    public static async Task<Results<Ok<ValidatedPayload<PayPeriod>>, NotFound<ValidatedResponse>, BadRequest<ValidatedForm<ValidationFailures>>>>
        UpdatePayPeriodAsync(
            ITimekeepingUnitOfWork unitOfWork,
            [FromRoute(Name = "payPeriodId")] int payPeriodId,
            [FromBody] PayPeriodCreateEdit input)
    {
        var result = await PayPeriodUpsertService.UpdatePayPeriodAsync(unitOfWork, payPeriodId, input);
        if (result.IsSuccess)
            return TypedResults.Ok(ValidatedPayload<PayPeriod>.FromPayload(result.Value));
        else
            return result.Error switch
            {
                PayPeriodNotFound => TypedResults.NotFound(ValidatedResponse.NotFound),
                PayPeriodInvalid(var f) => TypedResults.BadRequest(new ValidatedForm<ValidationFailures>
                {
                    IsSuccess = false,
                    IsInternalError = false,
                    ValidationFailures = f
                }),
                _ => throw new InvalidOperationException()
            };
    }

    //TODO: Only allow pay periods to be deleted if they have not yet been submitted.
    public static async Task<Results<Ok<ValidatedResponse>, NotFound<ValidatedResponse>, Conflict<ValidatedResponse>>>
        DeletePayPeriodAsync(
            ITimekeepingUnitOfWork unitOfWork,
            [FromRoute(Name = "payPeriodId")] int payPeriodId)
    {
        var deleteResult = unitOfWork.PayPeriodRepository.Delete(payPeriodId);
        if (deleteResult == DeleteResult.NotFound)
            return TypedResults.NotFound(ValidatedResponse.NotFound);

        await unitOfWork.SaveAsync().ConfigureAwait(false);
        return TypedResults.Ok(ValidatedResponse.Success);
    }

}
