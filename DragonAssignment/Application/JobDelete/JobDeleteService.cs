using CSharpFunctionalExtensions;
using DragonCommon.Application.Repositories;

namespace DragonAssignment.Application.JobDelete;

public abstract record JobDeleteFailure;
public record JobDeleteNotFound : JobDeleteFailure;
public record JobDeleteHasAssignment : JobDeleteFailure;

public static class JobDeleteService
{
    public static async Task<UnitResult<JobDeleteFailure>> DeleteJob(int jobId, IDragonPlacementUnitOfWork unitOfWork)
    {
        if (await unitOfWork.JobHasAnAssignment(jobId).ConfigureAwait(false))
            return UnitResult.Failure<JobDeleteFailure>(new JobDeleteHasAssignment());

        var deleteResult = unitOfWork.JobRepository.Delete(jobId);
        if (deleteResult == DeleteResult.NotFound)
            return UnitResult.Failure<JobDeleteFailure>(new JobDeleteNotFound());

        await unitOfWork.SaveAsync().ConfigureAwait(false);
        return UnitResult.Success<JobDeleteFailure>();
    }
}
