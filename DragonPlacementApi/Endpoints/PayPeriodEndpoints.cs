using DragonPlacementApi.Poco;
using DragonPlacementDataLayer.Models;
using DragonPlacementDataLayer.Repositories;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace DragonPlacementApi.Endpoints;

public class PayPeriodEndpoints
{
    public static async Task<Results<Ok<ValidatedPayload<PayPeriod>>, NotFound<ValidatedResponse>>>
        GetPayPeriodAsync(
            IAssignmentUnitOfWork unitOfWork,
            [FromRoute(Name = "payPeriodId")] int payPeriodId)
    {
        //TODO: Create a method that includes the HoursWorked records with the PayPeriod.
        var entry = await unitOfWork.PayPeriodRepository.GetByID(payPeriodId).ConfigureAwait(false);
        return entry == null
            ? TypedResults.NotFound(ValidatedResponse.NotFound)
            : TypedResults.Ok(ValidatedPayload<PayPeriod>.FromPayload(entry));
    }

    public static async Task<Results<Ok<ValidatedPayload<PayPeriod>>, BadRequest<ValidatedForm<PayPeriodValidationFailures>>>>
        CreatePayPeriodAsync(
            IAssignmentUnitOfWork unitOfWork,
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
            IAssignmentUnitOfWork unitOfWork,
            [FromRoute(Name = "payPeriodId")] int payPeriodId,
            [FromBody] PayPeriodCreateEdit input)
    {
        var entry = await unitOfWork.PayPeriodRepository.GetByID(payPeriodId).ConfigureAwait(false);
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

        foreach (var hw in input.HoursWorked)
        {
            if (hw.HoursWorkedId == 0)
            {
                entry.HoursWorked.Add(new HoursWorked
                {
                    AssignmentId = input.AssignmentId,
                    DragonId = input.DragonId,
                    StartDateTimeUnix = hw.StartDateTimeUnix,
                    EndDateTimeUnix = hw.EndDateTimeUnix
                });
            }
            else
            {
                var existing = await unitOfWork.HoursWorkedRepository.GetByID(hw.HoursWorkedId).ConfigureAwait(false);
                if (existing != null)
                {
                    existing.AssignmentId = input.AssignmentId;
                    existing.DragonId = input.DragonId;
                    existing.StartDateTimeUnix = hw.StartDateTimeUnix;
                    existing.EndDateTimeUnix = hw.EndDateTimeUnix;
                }
            }
        }

        await unitOfWork.SaveAsync().ConfigureAwait(false);
        return TypedResults.Ok(ValidatedPayload<PayPeriod>.FromPayload(entry));
    }

    public static async Task<Results<Ok<ValidatedResponse>, NotFound<ValidatedResponse>, Conflict<ValidatedResponse>>>
        DeletePayPeriodAsync(
            IAssignmentUnitOfWork unitOfWork,
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
