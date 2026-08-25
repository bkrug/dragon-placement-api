using CSharpFunctionalExtensions;
using DragonBilling.Domain.Models;
using DragonCommon.Domain.Poco;

namespace DragonBilling.Application.WorkRequestUpsert;

public abstract record WorkRequestCreateFailure;
public record CustomerNotFound : WorkRequestCreateFailure;
public record WorkRequestInvalid(ValidationFailures Failures) : WorkRequestCreateFailure;

public abstract record WorkRequestEditFailure;
public record WorkRequestNotFound : WorkRequestEditFailure;
public record WorkRequestNotInDraftStatus : WorkRequestEditFailure;
public record WorkRequestEditInvalid(ValidationFailures Failures) : WorkRequestEditFailure;

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

    public static async Task<Result<WorkRequest, WorkRequestEditFailure>> EditWorkRequest(
        IBillingUnitOfWork unitOfWork, WorkRequestCreateEdit input, int workRequestId)
    {
        var existing = await unitOfWork.WorkRequestRepository.GetByID(workRequestId).ConfigureAwait(false);
        if (existing == null)
            return Result.Failure<WorkRequest, WorkRequestEditFailure>(new WorkRequestNotFound());
        if (!existing.IsEditable)
            return Result.Failure<WorkRequest, WorkRequestEditFailure>(new WorkRequestNotInDraftStatus());

        return await WorkRequestCreateEditMapper.ApplyTo(input, existing)
            .Bind(workRequest => workRequest.Validate())
            .MapError(e => (WorkRequestEditFailure)new WorkRequestEditInvalid(e))
            .Tap(async _ => await unitOfWork.SaveChangesAsync().ConfigureAwait(false));
    }
}
