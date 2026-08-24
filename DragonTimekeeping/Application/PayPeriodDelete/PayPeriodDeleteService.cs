using CSharpFunctionalExtensions;
using DragonCommon.Domain.Poco;

namespace DragonTimekeeping.Application.PayPeriodDelete;

public abstract record PayPeriodDeleteFailure;
public record PayPeriodDeleteNotFound : PayPeriodDeleteFailure;
public record PayPeriodDeleteInvalid(ValidationFailures Failures) : PayPeriodDeleteFailure;

public static class PayPeriodDeleteService
{
    public static async Task<UnitResult<PayPeriodDeleteFailure>> DeletePayPeriodAsync(
        ITimekeepingUnitOfWork unitOfWork,
        int payPeriodId)
    {
        var existing = await unitOfWork.PayPeriodRepository.GetByID(payPeriodId).ConfigureAwait(false);
        if (existing == null)
            return UnitResult.Failure<PayPeriodDeleteFailure>(new PayPeriodDeleteNotFound());

        return await existing.EnsureDeletable()
            .Tap(() =>
            {
                unitOfWork.PayPeriodRepository.Delete(payPeriodId);
                return unitOfWork.SaveChangesAsync();
            })
            .MapError(e => (PayPeriodDeleteFailure)new PayPeriodDeleteInvalid(e))
            .ConfigureAwait(false);
    }
}
