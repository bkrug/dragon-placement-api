using CommonDataLayer.Repositories;
using DragonPlacementApi.Extensions;
using DragonPlacementApi.Poco;
using DragonPlacementDataLayer.Repositories;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using TimekeepingDataLayer.Models;
using TimekeepingDataLayer.Repositories;

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

    public static Ok<ValidatedPayload<List<ValidPaySpan>>> GetValidPayPeriods(
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
            .Select(startDateUnix => new ValidPaySpan
            {
                StartDate = DateTimeOffset.FromUnixTimeSeconds(startDateUnix).UtcDateTime.ToIsoDateString(),
                EndDate = DateTimeOffset.FromUnixTimeSeconds(startDateUnix).AddDays(6).UtcDateTime.ToIsoDateString()
            })
            .ToList();

        return TypedResults.Ok(ValidatedPayload<List<ValidPaySpan>>.FromPayload(candidates));
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

    public static async Task<Results<Ok<ValidatedPayload<PayPeriodView>>, NotFound<ValidatedResponse>>>
        GetPayPeriodNewAsync(
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
            DragonId = entry.DragonId,
            StartDate = UnixDateConvert.ToIsoDate(entry.StartDateUnix),
            EndDate = UnixDateConvert.ToIsoDate(entry.EndDateUnix),
            SubmissionStatus = entry.SubmissionStatus,
            DragonName = $"{assignment?.Dragon.GivenName} {assignment?.Dragon.FamilyName}",
            AssignmentDescription = $"{assignment?.Job.JobTitle} at {assignment?.Job.EmployerName}",
            HoursWorked = entry.HoursWorked.Select(hw => new HoursWorkedView
            {
                StartDateTime = UnixDateConvert.ToIsoDateTime(hw.StartDateTimeUnix),
                EndDateTime = UnixDateConvert.ToIsoDateTime(hw.EndDateTimeUnix)
            }).ToList()
        };
        return TypedResults.Ok(ValidatedPayload<PayPeriodView>.FromPayload(transformedEntry));
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
                    StartDateTimeUnix = inputHw.StartDateTimeUnix,
                    EndDateTimeUnix = inputHw.EndDateTimeUnix
                });
            }
            else
            {
                existingClockPunch.StartDateTimeUnix = inputHw.StartDateTimeUnix;
                existingClockPunch.EndDateTimeUnix = inputHw.EndDateTimeUnix;
            }
        }

        await unitOfWork.SaveAsync().ConfigureAwait(false);
        return TypedResults.Ok(ValidatedPayload<PayPeriod>.FromPayload(entry));
    }

    public static async Task<Results<Ok<ValidatedPayload<PayPeriod>>, BadRequest<ValidatedForm<PayPeriodValidationFailuresNew>>>>
        CreatePayPeriodNewAsync(
            ITimekeepingUnitOfWork unitOfWork,
            [FromBody] PayPeriodCreateEditNew input)
    {
        return TypedResults.Ok(ValidatedPayload<PayPeriod>.FromPayload(new PayPeriod()));
    }

    public static async Task<Results<Ok<ValidatedPayload<PayPeriod>>, NotFound<ValidatedResponse>, BadRequest<ValidatedForm<PayPeriodValidationFailuresNew>>>>
        UpdatePayPeriodNewAsync(
            ITimekeepingUnitOfWork unitOfWork,
            [FromRoute(Name = "payPeriodId")] int payPeriodId,
            [FromBody] PayPeriodCreateEditNew input)
    {
        return TypedResults.Ok(ValidatedPayload<PayPeriod>.FromPayload(new PayPeriod()));
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
