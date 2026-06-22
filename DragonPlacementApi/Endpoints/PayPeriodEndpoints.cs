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
        var validationFailures = ValidatePayPeriodNew(input);
        if (validationFailures != null)
            return TypedResults.BadRequest(validationFailures);

        var payPeriod = new PayPeriod
        {
            AssignmentId = input.AssignmentId,
            DragonId = input.DragonId,
            StartDateUnix = UnixDateConvert.FromIsoDate(input.StartDate),
            EndDateUnix = UnixDateConvert.FromIsoDate(input.EndDate),
            SubmissionStatus = input.SubmissionStatus,
            HoursWorked = input.HoursWorked.Select(hw => new HoursWorked
            {
                StartDateTimeUnix = UnixDateConvert.FromIsoDateTime(hw.StartDateTime),
                EndDateTimeUnix = UnixDateConvert.FromIsoDateTime(hw.EndDateTime)
            }).ToList()
        };
        unitOfWork.PayPeriodRepository.Insert(payPeriod);

        await unitOfWork.SaveAsync().ConfigureAwait(false);
        return TypedResults.Ok(ValidatedPayload<PayPeriod>.FromPayload(payPeriod));
    }

    public static async Task<Results<Ok<ValidatedPayload<PayPeriod>>, NotFound<ValidatedResponse>, BadRequest<ValidatedForm<PayPeriodValidationFailuresNew>>>>
        UpdatePayPeriodNewAsync(
            ITimekeepingUnitOfWork unitOfWork,
            [FromRoute(Name = "payPeriodId")] int payPeriodId,
            [FromBody] PayPeriodCreateEditNew input)
    {
        var entry = await unitOfWork.GetPayPeriodWithHoursWorkedAsync(payPeriodId).ConfigureAwait(false);
        if (entry == null)
            return TypedResults.NotFound(ValidatedResponse.NotFound);

        var validationFailures = ValidatePayPeriodNew(input);
        if (validationFailures != null)
            return TypedResults.BadRequest(validationFailures);

        var incomingStarts = input.HoursWorked.Select(hw => UnixDateConvert.FromIsoDateTime(hw.StartDateTime)).ToList();
        var deletedHours = entry.HoursWorked
            .Where(existingHw => !incomingStarts.Contains(existingHw.StartDateTimeUnix))
            .ToList();
        foreach (var recToDelete in deletedHours)
            entry.HoursWorked.Remove(recToDelete);

        entry.AssignmentId = input.AssignmentId;
        entry.DragonId = input.DragonId;
        entry.StartDateUnix = UnixDateConvert.FromIsoDate(input.StartDate);
        entry.EndDateUnix = UnixDateConvert.FromIsoDate(input.EndDate);
        entry.SubmissionStatus = input.SubmissionStatus;

        foreach (var inputHw in input.HoursWorked)
        {
            var startUnix = UnixDateConvert.FromIsoDateTime(inputHw.StartDateTime);
            var endUnix = UnixDateConvert.FromIsoDateTime(inputHw.EndDateTime);
            var existingClockPunch = entry.HoursWorked.FirstOrDefault(h => h.StartDateTimeUnix == startUnix);
            if (existingClockPunch == null)
            {
                entry.HoursWorked.Add(new HoursWorked
                {
                    StartDateTimeUnix = startUnix,
                    EndDateTimeUnix = endUnix
                });
            }
            else
            {
                existingClockPunch.StartDateTimeUnix = startUnix;
                existingClockPunch.EndDateTimeUnix = endUnix;
            }
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

    private static ValidatedForm<PayPeriodValidationFailuresNew>? ValidatePayPeriodNew(PayPeriodCreateEditNew input)
    {
        var failures = new PayPeriodValidationFailuresNew();

        if (!DateTime.TryParse(input.StartDate, out var parsedStart))
            failures.StartDate = "must be an ISO Date";
        else if (parsedStart.DayOfWeek != DayOfWeek.Monday)
            failures.StartDate = "must be a Monday";
        else if (parsedStart.TimeOfDay.TotalSeconds != 0)
            failures.StartDate = "must exclude time-of-day or be midnight UTC";

        if (!DateTime.TryParse(input.EndDate, out var parsedEnd))
            failures.EndDate = "must be an ISO Date";
        else if (parsedEnd.DayOfWeek != DayOfWeek.Sunday)
            failures.EndDate = "must be a Sunday";
        else if (parsedEnd.TimeOfDay.TotalSeconds != 0)
            failures.EndDate = "must exclude time-of-day or be midnight UTC";
        else if (parsedEnd <= parsedStart)
            failures.EndDate = "must be greater than StartDate";

        long startDateUnix = new DateTimeOffset(parsedStart, TimeSpan.Zero).ToUnixTimeSeconds();
        long endDateUnix = new DateTimeOffset(parsedEnd, TimeSpan.Zero).ToUnixTimeSeconds();

        failures.HoursWorked = input.HoursWorked
            .Select(hw =>
            {
                var hwStartUnix = UnixDateConvert.FromIsoDateTime(hw.StartDateTime);
                var hwEndUnix = UnixDateConvert.FromIsoDateTime(hw.EndDateTime);

                var hwf = new HoursWorkedValidationFailures();
                if (hwStartUnix < startDateUnix)
                    hwf.StartDateTime = "Clock-in time is outside of the pay period";
                if (hwEndUnix >= endDateUnix + Const.SECONDS_IN_A_DAY)
                    hwf.EndDateTime = "Clock-out time is outside of the pay period";
                return hwf;
            })
            .Where(hwf => !string.IsNullOrEmpty(hwf.StartDateTime) || !string.IsNullOrEmpty(hwf.EndDateTime))
            .ToList();

        return !string.IsNullOrEmpty(failures.StartDate)
            || !string.IsNullOrEmpty(failures.EndDate)
            || failures.HoursWorked.Count > 0
            ? new ValidatedForm<PayPeriodValidationFailuresNew>
                {
                    IsSuccess = false,
                    IsInternalError = false,
                    ValidationFailures = failures
                }
            : null;
    }
}
