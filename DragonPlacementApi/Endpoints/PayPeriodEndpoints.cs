using CommonDataLayer.Repositories;
using DragonPlacementApi.Poco;
using DragonAssignmentApplication;
using DragonTimekeepingApplication;
using DragonTimekeepingDomain.Validation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using DragonTimekeepingDomain.Models;
using DragonTimekeepingApplication.Dto;

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
        var results = unitOfWork.GetPayPeriodsByAssignment(assignmentId).ToList();
        return new()
        {
            Offset = offset,
            Limit = limit,
            TotalRecords = results.Count,
            Data = results.Skip(offset).Take(limit).ToList()
        };
    }

    public static Ok<ValidatedPayload<List<ValidPaySpan>>> GetValidPayPeriods(
            ITimekeepingUnitOfWork unitOfWork,
            [FromRoute(Name = "unusedId")] int unusedId,
            [FromRoute(Name = "assignmentId")] int assignmentId)
    {
        var today = DateTime.UtcNow.Date;
        var daysToSubtract = ((int)today.DayOfWeek + 6) % 7;
        var monday = today.AddDays(-daysToSubtract);

        var existingStarts = unitOfWork.GetPayPeriodsByAssignment(assignmentId)
            .Select(pp => pp.StartDate)
            .ToHashSet();

        var candidates = Enumerable.Range(0, 4)
            .Select(weeksAgo => monday.AddDays(-7 * weeksAgo))
            .Where(startDate => !existingStarts.Contains(startDate))
            .Select(startDate => new ValidPaySpan
            {
                StartDate = startDate.ToString("yyyy-MM-dd"),
                EndDate = startDate.AddDays(6).ToString("yyyy-MM-dd")
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
        var (payPeriod, validationFailures) = PayPeriodApplicationValidator.GetPayPeriodModel(input);
        if (validationFailures != null)
            return TypedResults.BadRequest(new ValidatedForm<PayPeriodValidationFailures>
            {
                IsSuccess = false,
                IsInternalError = false,
                ValidationFailures = validationFailures
            });

        unitOfWork.InsertPayPeriod(payPeriod!);

        await unitOfWork.SaveAsync().ConfigureAwait(false);
        return TypedResults.Ok(ValidatedPayload<PayPeriod>.FromPayload(payPeriod!));
    }

    public static async Task<Results<Ok<ValidatedPayload<PayPeriod>>, NotFound<ValidatedResponse>, BadRequest<ValidatedForm<PayPeriodValidationFailures>>>>
        UpdatePayPeriodAsync(
            ITimekeepingUnitOfWork unitOfWork,
            [FromRoute(Name = "payPeriodId")] int payPeriodId,
            [FromBody] PayPeriodCreateEdit input)
    {
        var entry = await unitOfWork.GetPayPeriodWithHoursWorkedAsync(payPeriodId).ConfigureAwait(false);
        if (entry == null)
            return TypedResults.NotFound(ValidatedResponse.NotFound);

        var (parsedPayPeriod, validationFailures) = PayPeriodApplicationValidator.GetPayPeriodModel(input);
        if (validationFailures != null)
            return TypedResults.BadRequest(new ValidatedForm<PayPeriodValidationFailures>
            {
                IsSuccess = false,
                IsInternalError = false,
                ValidationFailures = validationFailures
            });

        var inputClockIns = parsedPayPeriod!.HoursWorked.ToList();

        var clockInsToDelete = entry.HoursWorked
            .Where(existingHw => !inputClockIns.Any(ih => ih.StartDateTime == existingHw.StartDateTime))
            .ToList();
        foreach (var recToDelete in clockInsToDelete)
            entry.HoursWorked.Remove(recToDelete);

        entry.AssignmentId = parsedPayPeriod.AssignmentId;
        entry.StartDate = parsedPayPeriod.StartDate;
        entry.EndDate = parsedPayPeriod.EndDate;
        entry.SubmissionStatus = parsedPayPeriod.SubmissionStatus;

        foreach (var inputClockIn in inputClockIns)
        {
            var existingClockPunch = entry.HoursWorked.FirstOrDefault(h => h.StartDateTime == inputClockIn.StartDateTime);
            if (existingClockPunch == null)
                entry.HoursWorked.Add(inputClockIn);
            else
                existingClockPunch.EndDateTime = inputClockIn.EndDateTime;
        }

        await unitOfWork.SaveAsync().ConfigureAwait(false);
        return TypedResults.Ok(ValidatedPayload<PayPeriod>.FromPayload(entry));
    }

    //TODO: Only allow pay periods to be deleted if they have not yet been submitted.
    public static async Task<Results<Ok<ValidatedResponse>, NotFound<ValidatedResponse>, Conflict<ValidatedResponse>>>
        DeletePayPeriodAsync(
            ITimekeepingUnitOfWork unitOfWork,
            [FromRoute(Name = "payPeriodId")] int payPeriodId)
    {
        var deleteResult = unitOfWork.DeletePayPeriod(payPeriodId);
        if (deleteResult == DeleteResult.NotFound)
            return TypedResults.NotFound(ValidatedResponse.NotFound);

        await unitOfWork.SaveAsync().ConfigureAwait(false);
        return TypedResults.Ok(ValidatedResponse.Success);
    }

}
