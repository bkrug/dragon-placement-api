using CSharpFunctionalExtensions;
using DragonBilling.Domain.Models;
using DragonCommon.Domain.Poco;

namespace DragonBilling.Application.WorkRequestUpsert;

public abstract record WorkRequestCreateFailure;
public record CustomerNotFound : WorkRequestCreateFailure;
public record WorkRequestInvalid(ValidationFailures Failures) : WorkRequestCreateFailure;

public static class WorkRequestUpsertService
{
    public static async Task<Result<WorkRequest, WorkRequestCreateFailure>> CreateWorkRequest(
        IBillingUnitOfWork unitOfWork, WorkRequestCreateEdit input, int customerId)
    {
        if (!await unitOfWork.CustomerExists(customerId).ConfigureAwait(false))
            return Result.Failure<WorkRequest, WorkRequestCreateFailure>(new CustomerNotFound());

        return await WorkRequestCreateEditMapper.ToWorkRequest(input, customerId)
            .Bind(workRequest => workRequest.Validate())
            .MapError(e => (WorkRequestCreateFailure)new WorkRequestInvalid(e))
            .Tap(workRequest => unitOfWork.WorkRequestRepository.Insert(workRequest))
            .Tap(async _ => await unitOfWork.SaveChangesAsync().ConfigureAwait(false));
    }
}
