using CSharpFunctionalExtensions;
using DragonTimekeepingApplication.Dto;
using DragonTimekeepingDomain.Models;
using DragonTimekeepingDomain.Validation;

namespace DragonTimekeepingApplication;

public static class PayPeriodService
{
    public static async Task<Result<PayPeriod, PayPeriodValidationFailures>> CreatePayPeriodAsync(
        ITimekeepingUnitOfWork unitOfWork,
        PayPeriodCreateEdit input)
    {
        return await PayPeriodParser.GetPayPeriodModel(input)
            .Tap(async payPeriod =>
            {
                unitOfWork.PayPeriodRepository.Insert(payPeriod);
                await unitOfWork.SaveAsync().ConfigureAwait(false);
            });
    }
}
