using CSharpFunctionalExtensions;
using DragonTimekeepingApplication.Dto;
using DragonTimekeepingDomain.Models;
using DragonTimekeepingDomain.Validation;

namespace DragonTimekeepingApplication;

public abstract record PayPeriodUpdateFailure;
public record PayPeriodNotFound : PayPeriodUpdateFailure;
public record PayPeriodInvalid(PayPeriodValidationFailures Failures) : PayPeriodUpdateFailure;

public static class PayPeriodService
{
    public static async Task<Result<PayPeriod, PayPeriodValidationFailures>> CreatePayPeriodAsync(
        ITimekeepingUnitOfWork unitOfWork,
        PayPeriodCreateEdit input)
    {
        return await PayPeriodTransformer.ToPayPeriodModel(input)
            .Bind(PayPeriodValidator.Validate)
            .Tap(async payPeriod =>
            {
                unitOfWork.PayPeriodRepository.Insert(payPeriod);
                await unitOfWork.SaveAsync().ConfigureAwait(false);
            });
    }

    public static async Task<Result<PayPeriod, PayPeriodUpdateFailure>> UpdatePayPeriodAsync(
        ITimekeepingUnitOfWork unitOfWork,
        int payPeriodId,
        PayPeriodCreateEdit input)
    {
        var existing = await unitOfWork.GetPayPeriodWithHoursWorkedAsync(payPeriodId).ConfigureAwait(false);
        if (existing == null)
            return Result.Failure<PayPeriod, PayPeriodUpdateFailure>(new PayPeriodNotFound());

        return await PayPeriodTransformer.ToPayPeriodModel(input)
            .Bind(PayPeriodValidator.Validate)
            .MapError(e => (PayPeriodUpdateFailure)new PayPeriodInvalid(e))
            .Map(parsedInput => EditPayPeriod(existing, parsedInput))
            .Tap(async _ => await unitOfWork.SaveAsync().ConfigureAwait(false));
    }

    private static PayPeriod EditPayPeriod(PayPeriod existing, PayPeriod input)
    {
        var inputClockIns = input.HoursWorked.ToList();

        //Delete child records no included in the input
        var clockInsToDelete = existing.HoursWorked
            .Where(existingHw => !inputClockIns.Any(ih => ih.StartDateTime == existingHw.StartDateTime))
            .ToList();
        foreach (var recToDelete in clockInsToDelete)
            existing.HoursWorked.Remove(recToDelete);

        //Update the fields in the parent record
        existing.AssignmentId = input.AssignmentId;
        existing.StartDate = input.StartDate;
        existing.EndDate = input.EndDate;
        existing.SubmissionStatus = input.SubmissionStatus;

        //Create or Update child records as specified by the input
        foreach (var inputClockIn in inputClockIns)
        {
            var existingClockPunch = existing.HoursWorked.FirstOrDefault(h => h.StartDateTime == inputClockIn.StartDateTime);
            if (existingClockPunch == null)
                existing.HoursWorked.Add(inputClockIn);
            else
                existingClockPunch.EndDateTime = inputClockIn.EndDateTime;
        }

        return existing;
    }
}
