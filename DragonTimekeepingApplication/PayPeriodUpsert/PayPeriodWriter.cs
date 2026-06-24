using CSharpFunctionalExtensions;
using DragonTimekeepingDomain.Models;
using DragonTimekeepingDomain.Validation;

namespace DragonTimekeepingApplication.PayPeriodUpsert;

public abstract record PayPeriodUpdateFailure;
public record PayPeriodNotFound : PayPeriodUpdateFailure;
public record PayPeriodInvalid(PayPeriodValidationFailures Failures) : PayPeriodUpdateFailure;

public static class PayPeriodWriter
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
            .Map(parsedInput => { existing.ApplyEdit(parsedInput); return existing; })
            .Tap(async _ => await unitOfWork.SaveAsync().ConfigureAwait(false));
    }
}
