using DragonCommonApplication.Repositories;
using DragonPlacementApi.Extensions;
using DragonPlacementApi.Poco;
using DragonAssignmentApplication;
using DragonTimekeepingApplication;
using DragonTimekeepingDomain.Validation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using DragonTimekeepingDomain.Models;
using DragonTimekeepingApplication.PayPeriodUpsert;

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

    /// <summary>
    /// Get all of the pay periods that the user is allowed to create new pay period records for.
    /// Do not allow the user to re-create an existing pay period for the current assignment.
    /// And a user can only go backwards so far in time if hours have not already been reported.
    /// </summary>
    public static Ok<ValidatedPayload<List<ValidPaySpan>>> GetValidPayPeriods(
            ITimekeepingUnitOfWork unitOfWork,
            [FromRoute(Name = "unusedId")] int unusedId,
            [FromRoute(Name = "assignmentId")] int assignmentId)
    {
        //The maximum number of weeks that the user is allowed to book at any given time.
        const int REPORTABLE_WEEKS = 4;

        var today = DateTime.UtcNow.Date;
        var daysToSubtract = ((int)today.DayOfWeek + 6) % 7;
        var monday = today.AddDays(-daysToSubtract);

        var existingStarts = unitOfWork.PayPeriodRepository
            .Get(pp => pp.AssignmentId == assignmentId)
            .Select(pp => pp.StartDate)
            .ToHashSet();

        var candidates = Enumerable.Range(0, REPORTABLE_WEEKS)
            .Select(weeksAgo => monday.AddDays(-7 * weeksAgo))
            .Where(startDate => !existingStarts.Contains(startDate))
            .Select(startDate => new ValidPaySpan
            {
                StartDate = startDate.ToIsoDateString(),
                EndDate = startDate.AddDays(6).ToIsoDateString()
            })
            .ToList();

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

        var transformedEntry = new PayPeriodView
        {
            AssignmentId = entry.AssignmentId,
            StartDate = entry.StartDate.ToString("yyyy-MM-dd"),
            EndDate = entry.EndDate.ToString("yyyy-MM-dd"),
            SubmissionStatus = entry.SubmissionStatus,
            DragonName = $"{assignment?.Dragon.GivenName} {assignment?.Dragon.FamilyName}",
            AssignmentDescription = $"{assignment?.Job.JobTitle} at {assignment?.Job.EmployerName}",
            HoursWorked = entry.HoursWorked.Select(hw => new HoursWorkedView
            {
                StartDateTime = hw.StartDateTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                EndDateTime = hw.EndDateTime.ToString("yyyy-MM-ddTHH:mm:ss")
            }).ToList()
        };
        return TypedResults.Ok(ValidatedPayload<PayPeriodView>.FromPayload(transformedEntry));
    }

    public static async Task<Results<Ok<ValidatedPayload<PayPeriod>>, BadRequest<ValidatedForm<PayPeriodValidationFailures>>>>
        CreatePayPeriodAsync(
            ITimekeepingUnitOfWork unitOfWork,
            [FromBody] PayPeriodCreateEdit input)
    {
        var creationResult = await PayPeriodWriter.CreatePayPeriodAsync(unitOfWork, input);
        if (creationResult.IsSuccess)
            return TypedResults.Ok(ValidatedPayload<PayPeriod>.FromPayload(creationResult.Value));
        else
            return TypedResults.BadRequest(new ValidatedForm<PayPeriodValidationFailures>
            {
                IsSuccess = false,
                IsInternalError = false,
                ValidationFailures = creationResult.Error
            });
    }

    public static async Task<Results<Ok<ValidatedPayload<PayPeriod>>, NotFound<ValidatedResponse>, BadRequest<ValidatedForm<PayPeriodValidationFailures>>>>
        UpdatePayPeriodAsync(
            ITimekeepingUnitOfWork unitOfWork,
            [FromRoute(Name = "payPeriodId")] int payPeriodId,
            [FromBody] PayPeriodCreateEdit input)
    {
        var result = await PayPeriodWriter.UpdatePayPeriodAsync(unitOfWork, payPeriodId, input);
        if (result.IsSuccess)
            return TypedResults.Ok(ValidatedPayload<PayPeriod>.FromPayload(result.Value));
        else
            return result.Error switch
            {
                PayPeriodNotFound => TypedResults.NotFound(ValidatedResponse.NotFound),
                PayPeriodInvalid(var f) => TypedResults.BadRequest(new ValidatedForm<PayPeriodValidationFailures>
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
