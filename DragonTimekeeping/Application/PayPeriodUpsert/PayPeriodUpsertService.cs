using CSharpFunctionalExtensions;
using DragonCommon.Domain.Poco;
using DragonTimekeeping.Domain.Models;

namespace DragonTimekeeping.Application.PayPeriodUpsert;

public abstract record PayPeriodUpdateFailure;
public record PayPeriodNotFound : PayPeriodUpdateFailure;
public record PayPeriodInvalid(ValidationFailures Failures) : PayPeriodUpdateFailure;

public static class PayPeriodUpsertService
{
    public static async Task<Result<PayPeriod, ValidationFailures>> CreatePayPeriodAsync(
        ITimekeepingUnitOfWork unitOfWork,
        PayPeriodCreateEdit input)
    {
        return await PayPeriodMapper.ToPayPeriodModel(input)
            .Bind(pp => pp.Validate())
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

        return await PayPeriodMapper.ToPayPeriodModel(input)
            .Bind(pp => pp.Validate())
            .MapError(e => (PayPeriodUpdateFailure)new PayPeriodInvalid(e))
            .Bind(parsedInput => existing.ApplyEdit(parsedInput)
                .MapError(e => (PayPeriodUpdateFailure)new PayPeriodInvalid(e))
                .Map(() => existing))
            .Tap(async _ => await unitOfWork.SaveAsync().ConfigureAwait(false));
    }
}
