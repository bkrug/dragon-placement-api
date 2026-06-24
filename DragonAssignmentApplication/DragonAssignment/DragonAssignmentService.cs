using CSharpFunctionalExtensions;
using DragonAssignmentDomain.Models;

namespace DragonAssignmentApplication.DragonAssignment;

public abstract record AssignmentFailure;
public record AssignmentJobNotFound : AssignmentFailure;
public record AssignmentOverlap(DateTime StartDate, DateTime EndDate) : AssignmentFailure;
public record AssignmentNotFound : AssignmentFailure;

public static class DragonAssignmentService
{
    public static async Task<Result<Assignment, AssignmentFailure>> AssignDragonToJob(
        int dragonId, int jobId, IDragonPlacementUnitOfWork unitOfWork)
    {
        var job = await unitOfWork.JobRepository.GetByID(jobId).ConfigureAwait(false);
        if (job == null)
            return Result.Failure<Assignment, AssignmentFailure>(new AssignmentJobNotFound());

        var firstConflict = unitOfWork.GetOverlappingAssignments(dragonId, job.StartDate, job.EndDate)
            .FirstOrDefault();
        if (firstConflict != null)
            return Result.Failure<Assignment, AssignmentFailure>(
                new AssignmentOverlap(firstConflict.StartDate, firstConflict.EndDate));

        var assignment = job.Assign(dragonId);
        unitOfWork.AssignmentRepository.Insert(assignment);
        await unitOfWork.SaveAsync().ConfigureAwait(false);

        return Result.Success<Assignment, AssignmentFailure>(assignment);
    }

    public static async Task<UnitResult<AssignmentFailure>> UnassignDragonFromJob(
        int dragonId, int jobId, IDragonPlacementUnitOfWork unitOfWork)
    {
        var assignment = unitOfWork.AssignmentRepository
            .Get(a => a.JobId == jobId && a.DragonId == dragonId)
            .FirstOrDefault();
        if (assignment == null)
            return UnitResult.Failure<AssignmentFailure>(new AssignmentNotFound());

        unitOfWork.AssignmentRepository.Delete(assignment);
        await unitOfWork.SaveAsync().ConfigureAwait(false);
        return UnitResult.Success<AssignmentFailure>();
    }
}
