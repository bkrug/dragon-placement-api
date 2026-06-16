using System.Net;
using DragonPlacementApi.Poco;
using DragonPlacementDataLayer.Models;
using DragonPlacementDataLayer.Repositories;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace DragonPlacementApi.Endpoints;

public class HoursWorkedEndpoints
{
    public static PagedData<HoursWorked> GetHoursWorked(
        IAssignmentUnitOfWork unitOfWork,
        [FromRoute(Name = "dragonId")] int dragonId,
        [FromQuery(Name = "assignmentId")] int? assignmentId = null,
        [FromQuery(Name = "offset")] int offset = 0,
        [FromQuery(Name = "limit")] int limit = 20)
    {
        var results = unitOfWork.HoursWorkedRepository.Get(
            hw => (assignmentId == null || hw.AssignmentId == assignmentId)
               && (hw.DragonId == dragonId));
        return new()
        {
            Offset = offset,
            Limit = limit,
            TotalRecords = results.Count(),
            Data = results.Skip(offset).Take(limit).ToList()
        };
    }

    public static PagedData<HoursWorked> GetHoursFromWeek(
        IAssignmentUnitOfWork unitOfWork,
        ILogger<AssignmentUnitOfWork> logger,
        [FromRoute(Name = "dragonId")] int dragonId,
        [FromRoute(Name = "assignmentId")] int assignmentId,
        [FromRoute(Name = "startOfWeekUnix")] int startOfWeekUnix)
    {
        const int LENGTH_OF_WEEK = 7 * 24 * 3600;
        var results = unitOfWork.HoursWorkedRepository
            .Get(hw => hw.AssignmentId == assignmentId
                    && hw.DragonId == dragonId
                    && hw.StartDateTimeUnix >= startOfWeekUnix
                    && hw.StartDateTimeUnix < startOfWeekUnix + LENGTH_OF_WEEK)
            .OrderBy(hw => hw.StartDateTimeUnix)
            .Take(100)
            .ToList();
        if (results.Count > 14)
        {
            logger.LogWarning("Dragon {DragonId} had {totalRecords} records of work time in the week of {Week}, no more than 14 are expected.",
                dragonId,
                results.Count,
                DateTimeOffset.FromUnixTimeSeconds(startOfWeekUnix).ToString()
            );
        }
        return new()
        {
            Offset = 0,
            Limit = int.MaxValue,
            TotalRecords = results.Count,
            Data = results
        };
    }    

    public static async Task<Results<Ok<ValidatedPayload<HoursWorked>>, NotFound<ValidatedResponse>>>
        GetHoursWorkedEntryAsync(
            IAssignmentUnitOfWork unitOfWork,
            [FromRoute(Name = "hoursWorkedId")] int hoursWorkedId)
    {
        var entry = await unitOfWork.HoursWorkedRepository.GetByID(hoursWorkedId).ConfigureAwait(false);
        return entry == null
            ? TypedResults.NotFound(ValidatedResponse.NotFound)
            : TypedResults.Ok(ValidatedPayload<HoursWorked>.FromPayload(entry));
    }

    public static async Task<Results<Ok<ValidatedPayload<HoursWorked>>, BadRequest<ValidatedForm<HoursWorkedValidationFailures>>>>
        CreateHoursWorkedAsync(
            IAssignmentUnitOfWork unitOfWork,
            [FromBody] HoursWorkedCreateEdit input)
    {
        var validationFailures = ValidateHoursWorked(input);
        if (validationFailures != null)
            return TypedResults.BadRequest(validationFailures);

        var entry = new HoursWorked
        {
            AssignmentId = input.AssignmentId,
            DragonId = input.DragonId,
            StartDateTimeUnix = input.StartDateTimeUnix,
            EndDateTimeUnix = input.EndDateTimeUnix
        };
        unitOfWork.HoursWorkedRepository.Insert(entry);
        await unitOfWork.SaveAsync().ConfigureAwait(false);
        return TypedResults.Ok(ValidatedPayload<HoursWorked>.FromPayload(entry));
    }

    public static async Task<Results<Ok<ValidatedPayload<HoursWorked>>, NotFound<ValidatedResponse>, BadRequest<ValidatedForm<HoursWorkedValidationFailures>>>>
        UpdateHoursWorkedAsync(
            IAssignmentUnitOfWork unitOfWork,
            [FromRoute(Name = "hoursWorkedId")] int hoursWorkedId,
            [FromBody] HoursWorkedCreateEdit input)
    {
        var entry = await unitOfWork.HoursWorkedRepository.GetByID(hoursWorkedId).ConfigureAwait(false);
        if (entry == null)
            return TypedResults.NotFound(ValidatedResponse.NotFound);

        var validationFailures = ValidateHoursWorked(input);
        if (validationFailures != null)
            return TypedResults.BadRequest(validationFailures);

        entry.AssignmentId = input.AssignmentId;
        entry.DragonId = input.DragonId;
        entry.StartDateTimeUnix = input.StartDateTimeUnix;
        entry.EndDateTimeUnix = input.EndDateTimeUnix;
        await unitOfWork.SaveAsync().ConfigureAwait(false);
        return TypedResults.Ok(ValidatedPayload<HoursWorked>.FromPayload(entry));
    }

    public static async Task<Results<Ok<ValidatedResponse>, NotFound<ValidatedResponse>>>
        DeleteHoursWorkedAsync(
            IAssignmentUnitOfWork unitOfWork,
            [FromRoute(Name = "hoursWorkedId")] int hoursWorkedId)
    {
        var deleteResult = unitOfWork.HoursWorkedRepository.Delete(hoursWorkedId);
        if (deleteResult == DeleteResult.NotFound)
            return TypedResults.NotFound(ValidatedResponse.NotFound);

        await unitOfWork.SaveAsync().ConfigureAwait(false);
        return TypedResults.Ok(ValidatedResponse.Success);
    }

    private static ValidatedForm<HoursWorkedValidationFailures>? ValidateHoursWorked(HoursWorkedCreateEdit input)
    {
        var failures = new HoursWorkedValidationFailures();
        var duration = input.EndDateTimeUnix - input.StartDateTimeUnix;

        if (duration < 0)
            failures.EndDateTimeUnix = "Cannot clock out before clocking in";
        else if (duration < 60 || duration > 86_400)
            failures.EndDateTimeUnix = "Cannot log less than 1 minute or more than 24 hours as a single log entry";

        if (!string.IsNullOrEmpty(failures.EndDateTimeUnix))
            return new ValidatedForm<HoursWorkedValidationFailures>
            {
                IsSuccess = false,
                IsInternalError = false,
                ValidationFailures = failures
            };

        return null;
    }
}
