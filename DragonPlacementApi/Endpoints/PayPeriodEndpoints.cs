using CommonDataLayer.Repositories;
using DragonPlacementApi.Extensions;
using DragonPlacementApi.Poco;
using DragonPlacementDataLayer;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using TimekeepingDataLayer;
using TimekeepingDataLayer.Models;

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
                filter: pp => pp.AssignmentId == assignmentId,
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
            .Get(pp => pp.AssignmentId == assignmentId)
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
        var validationFailures = ValidatePayPeriodNew(input);
        if (validationFailures != null)
            return TypedResults.BadRequest(validationFailures);

        var payPeriod = new PayPeriod
        {
            AssignmentId = input.AssignmentId,
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

    public static async Task<Results<Ok<ValidatedPayload<PayPeriod>>, NotFound<ValidatedResponse>, BadRequest<ValidatedForm<PayPeriodValidationFailures>>>>
        UpdatePayPeriodAsync(
            ITimekeepingUnitOfWork unitOfWork,
            [FromRoute(Name = "payPeriodId")] int payPeriodId,
            [FromBody] PayPeriodCreateEdit input)
    {
        var entry = await unitOfWork.GetPayPeriodWithHoursWorkedAsync(payPeriodId).ConfigureAwait(false);
        if (entry == null)
            return TypedResults.NotFound(ValidatedResponse.NotFound);

        var validationFailures = ValidatePayPeriodNew(input);
        if (validationFailures != null)
            return TypedResults.BadRequest(validationFailures);

        var inputClockIns = input.HoursWorked
            .Select(hw => new HoursWorked {
                StartDateTimeUnix = UnixDateConvert.FromIsoDateTime(hw.StartDateTime),
                EndDateTimeUnix = UnixDateConvert.FromIsoDateTime(hw.EndDateTime)
            })
            .ToList();

        var clockInsToDelete = entry.HoursWorked
            .Where(existingHw => !inputClockIns.Any(ih => ih.StartDateTimeUnix == existingHw.StartDateTimeUnix))
            .ToList();
        foreach (var recToDelete in clockInsToDelete)
            entry.HoursWorked.Remove(recToDelete);

        entry.AssignmentId = input.AssignmentId;
        entry.StartDateUnix = UnixDateConvert.FromIsoDate(input.StartDate);
        entry.EndDateUnix = UnixDateConvert.FromIsoDate(input.EndDate);
        entry.SubmissionStatus = input.SubmissionStatus;

        foreach (var inputClockIn in inputClockIns)
        {
            var existingClockPunch = entry.HoursWorked.FirstOrDefault(h => h.StartDateTimeUnix == inputClockIn.StartDateTimeUnix);
            if (existingClockPunch == null)
                entry.HoursWorked.Add(inputClockIn);
            else
                existingClockPunch.EndDateTimeUnix = inputClockIn.EndDateTimeUnix;
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

    private static ValidatedForm<PayPeriodValidationFailures>? ValidatePayPeriodNew(PayPeriodCreateEdit input)
    {
        var failures = new PayPeriodValidationFailures();

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

        long payPeriodStartUnix = new DateTimeOffset(parsedStart, TimeSpan.Zero).ToUnixTimeSeconds();
        long payPeriodEndUnix = new DateTimeOffset(parsedEnd, TimeSpan.Zero).ToUnixTimeSeconds();

        var parsedHoursWorked = input.HoursWorked
            .Select(hw => (
                StartUnix: UnixDateConvert.FromIsoDateTime(hw.StartDateTime),
                EndUnix: UnixDateConvert.FromIsoDateTime(hw.EndDateTime)
            ))
            .ToList();

        failures.HoursWorked = parsedHoursWorked
            .Select((hw, index) =>
            {
                var hwf = new HoursWorkedValidationFailures()
                {
                    Index = index,
                };
                if (hw.StartUnix < payPeriodStartUnix)
                    hwf.RowValidationMessage = "Clock-in time is outside of the pay period";
                else if (hw.EndUnix >= payPeriodEndUnix + Const.SECONDS_IN_A_DAY)
                    hwf.RowValidationMessage = "Clock-out time is outside of the pay period";
                else if (parsedHoursWorked.Where((other, i) => i != index && hw.StartUnix < other.EndUnix && other.StartUnix < hw.EndUnix).Any())
                    hwf.RowValidationMessage = "Overlaps with another hours-worked record";
                return hwf;
            })
            .Where(hwf => !string.IsNullOrEmpty(hwf.StartDateTime) || !string.IsNullOrEmpty(hwf.EndDateTime) || !string.IsNullOrEmpty(hwf.RowValidationMessage))
            .ToList();

        return !string.IsNullOrEmpty(failures.StartDate)
            || !string.IsNullOrEmpty(failures.EndDate)
            || failures.HoursWorked.Count > 0
            ? new ValidatedForm<PayPeriodValidationFailures>
                {
                    IsSuccess = false,
                    IsInternalError = false,
                    ValidationFailures = failures
                }
            : null;
    }
}
