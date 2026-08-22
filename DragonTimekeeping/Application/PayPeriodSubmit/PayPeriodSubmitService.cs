using CSharpFunctionalExtensions;
using DragonCommon.Domain.Poco;

namespace DragonTimekeeping.Application.PayPeriodSubmit;

public abstract record PayPeriodSubmitFailure;
public record PayPeriodSubmitNotFound : PayPeriodSubmitFailure;
public record PayPeriodSubmitInvalid(ValidationFailures Failures) : PayPeriodSubmitFailure;

public static class PayPeriodSubmitService
{
    public static async Task<UnitResult<PayPeriodSubmitFailure>> SubmitPayPeriodAsync(
        ITimekeepingUnitOfWork unitOfWork,
        int payPeriodId)
    {
        var existing = await unitOfWork.PayPeriodRepository.GetByID(payPeriodId).ConfigureAwait(false);
        if (existing == null)
            return UnitResult.Failure<PayPeriodSubmitFailure>(new PayPeriodSubmitNotFound());

        return await existing.Submit()
            .Tap(() => unitOfWork.SaveAsync())
            .MapError(e => (PayPeriodSubmitFailure)new PayPeriodSubmitInvalid(e))
            .ConfigureAwait(false);
    }
}
