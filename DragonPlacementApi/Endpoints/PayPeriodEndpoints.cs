using DragonPlacementApi.Poco;
using DragonPlacementDataLayer.Models;
using DragonPlacementDataLayer.Repositories;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace DragonPlacementApi.Endpoints;

public class PayPeriodEndpoints
{
    public static PagedData<PayPeriod> GetPayPeriods(
            ITimekeepingUnitOfWork unitOfWork,
            [FromRoute(Name = "dragonId")] int dragonId,
            [FromRoute(Name = "assignmentId")] int assignmentId,
            [FromQuery(Name = "offset")] int offset = 0,
            [FromQuery(Name = "limit")] int limit = 20)
    {
        var results = unitOfWork.PayPeriodRepository
            .Get(
                filter: pp => pp.DragonId == dragonId && pp.AssignmentId == assignmentId,
                orderBy: q => q.OrderByDescending(pp => pp.StartDateUnix)
            );
        return new()
        {
            Offset = offset,
            Limit = limit,
            TotalRecords = results.Count(),
            Data = results.Skip(offset).Take(limit).ToList()
        };
    }

    public static Ok<ValidatedPayload<List<PayPeriod>>> GetValidPayPeriods(
            ITimekeepingUnitOfWork unitOfWork,
            [FromRoute(Name = "dragonId")] int dragonId,
            [FromRoute(Name = "assignmentId")] int assignmentId)
    {
        var today = DateTime.UtcNow.Date;
        var daysToSubtract = ((int)today.DayOfWeek + 6) % 7;
        var mondayUnix = new DateTimeOffset(today.AddDays(-daysToSubtract), TimeSpan.Zero).ToUnixTimeSeconds();

        var existingStarts = unitOfWork.PayPeriodRepository
            .Get(pp => pp.DragonId == dragonId && pp.AssignmentId == assignmentId)
            .Select(pp => pp.StartDateUnix)
            .ToHashSet();

        const long SECONDS_IN_A_WEEK = 7 * Const.SECONDS_IN_A_DAY;
        var candidates = Enumerable.Range(0, 4)
            .Select(weeksAgo => mondayUnix - weeksAgo * SECONDS_IN_A_WEEK)
            .Where(startDateUnix => !existingStarts.Contains(startDateUnix))
            .Select(startDateUnix => new PayPeriod
            {
                AssignmentId = assignmentId,
                DragonId = dragonId,
                StartDateUnix = startDateUnix,
                EndDateUnix = startDateUnix + 6 * Const.SECONDS_IN_A_DAY
            })
            .ToList();

        return TypedResults.Ok(ValidatedPayload<List<PayPeriod>>.FromPayload(candidates));
    }

    public static async Task<Results<Ok<ValidatedPayload<PayPeriod>>, NotFound<ValidatedResponse>>>
        GetPayPeriodAsync(
            ITimekeepingUnitOfWork unitOfWork,
            [FromRoute(Name = "payPeriodId")] int payPeriodId)
    {
        var entry = await unitOfWork.GetPayPeriodWithHoursWorkedAsync(payPeriodId).ConfigureAwait(false);
        return entry == null
            ? TypedResults.NotFound(ValidatedResponse.NotFound)
            : TypedResults.Ok(ValidatedPayload<PayPeriod>.FromPayload(entry));
    }

    public static async Task<Results<Ok<ValidatedPayload<PayPeriod>>, BadRequest<ValidatedForm<PayPeriodValidationFailures>>>>
        CreatePayPeriodAsync(
            ITimekeepingUnitOfWork unitOfWork,
            [FromBody] PayPeriodCreateEdit input)
    {
        var validationFailures = ValidatePayPeriod(input);
        if (validationFailures != null)
            return TypedResults.BadRequest(validationFailures);

        var payPeriod = new PayPeriod
        {
            AssignmentId = input.AssignmentId,
            DragonId = input.DragonId,
            StartDateUnix = input.StartDateUnix,
            EndDateUnix = input.EndDateUnix,
            SubmissionStatus = input.SubmissionStatus,
            HoursWorked = input.HoursWorked.Select(hw => new HoursWorked
            {
                AssignmentId = input.AssignmentId,
                DragonId = input.DragonId,
                StartDateTimeUnix = hw.StartDateTimeUnix,
                EndDateTimeUnix = hw.EndDateTimeUnix
            }).ToList()
        };
        unitOfWork.PayPeriodRepository.Insert(payPeriod);

        await unitOfWork.SaveAsync().ConfigureAwait(false);
        return TypedResults.Ok(ValidatedPayload<PayPeriod>.FromPayload(payPeriod));
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

        var validationFailures = ValidatePayPeriod(input);
        if (validationFailures != null)
            return TypedResults.BadRequest(validationFailures);

        entry.AssignmentId = input.AssignmentId;
        entry.DragonId = input.DragonId;
        entry.StartDateUnix = input.StartDateUnix;
        entry.EndDateUnix = input.EndDateUnix;
        entry.SubmissionStatus = input.SubmissionStatus;

        var recordsNotToDelete = input.HoursWorked.Select(inputHw => inputHw.StartDateTimeUnix).ToList();
        var deletedHours = entry.HoursWorked
            .Where(existingHw => !recordsNotToDelete.Contains(existingHw.StartDateTimeUnix))
            .ToList();
        foreach (var recToDelete in deletedHours)
            entry.HoursWorked.Remove(recToDelete);

        foreach (var inputHw in input.HoursWorked)
        {
            var existingClockPunch = entry.HoursWorked.FirstOrDefault(h => h.StartDateTimeUnix == inputHw.StartDateTimeUnix);
            if (existingClockPunch == null)
            {
                entry.HoursWorked.Add(new HoursWorked
                {
                    AssignmentId = input.AssignmentId,
                    DragonId = input.DragonId,
                    StartDateTimeUnix = inputHw.StartDateTimeUnix,
                    EndDateTimeUnix = inputHw.EndDateTimeUnix
                });
            }
            else
            {
                existingClockPunch.AssignmentId = input.AssignmentId;
                existingClockPunch.DragonId = input.DragonId;
                existingClockPunch.StartDateTimeUnix = inputHw.StartDateTimeUnix;
                existingClockPunch.EndDateTimeUnix = inputHw.EndDateTimeUnix;
            }
        }

        await unitOfWork.SaveAsync().ConfigureAwait(false);
        return TypedResults.Ok(ValidatedPayload<PayPeriod>.FromPayload(entry));
    }

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

    private static ValidatedForm<PayPeriodValidationFailures>? ValidatePayPeriod(PayPeriodCreateEdit input)
    {
        var failures = new PayPeriodValidationFailures();

        if (input.StartDateUnix % Const.SECONDS_IN_A_DAY != 0)
            failures.StartDateUnix = "must be midnight UTC";
        if (input.EndDateUnix % Const.SECONDS_IN_A_DAY != 0)
            failures.EndDateUnix = "must be midnight UTC";
        else if (input.EndDateUnix <= input.StartDateUnix)
            failures.EndDateUnix = "must be greater than StartDateUnix";
        if (input.HoursWorked.Any(hw => hw.StartDateTimeUnix < input.StartDateUnix))
            failures.HoursWorkedStartDateTimeUnix = "all must be greater than or equal to pay period StartDateUnix";
        if (input.HoursWorked.Any(hw => hw.EndDateTimeUnix >= input.EndDateUnix + Const.SECONDS_IN_A_DAY))
            failures.HoursWorkedEndDateTimeUnix = "all must be less than pay period EndDateUnix plus one day";

        if (failures.StartDateUnix != null || failures.EndDateUnix != null
            || failures.HoursWorkedStartDateTimeUnix != null || failures.HoursWorkedEndDateTimeUnix != null)
            return new ValidatedForm<PayPeriodValidationFailures>
            {
                IsSuccess = false,
                IsInternalError = false,
                ValidationFailures = failures
            };

        return null;
    }
}
