using DragonCommonApplication.Repositories;
using DragonAssignment.Domain.Enum;
using DragonAssignment.Domain.Models;
using DragonAssignment.Domain.Poco;

namespace DragonAssignment.Application;

/// <summary>
/// You see more than one UnitOfWork context in this git-repository.
///
/// Each UnitOfWork uses GenericRepositories,
/// which should allow us to do write-operations and select-by-id-operations agaist single tables with little boiler-plate code.
///
/// UnitOfWork classes also contain multiple methods for doing select-queries,
/// which need to be hand-written because their patterns are less consistent than create/update/delete or select-by-id.
/// Keeping these select-queries in the unit-of-work class means that we don't need to keep writing subclasses of the GenericRepository.
/// It is also a more logical location for these queries because they often involve joins between two or more tables.
/// </summary>
public interface IDragonPlacementUnitOfWork
{
    IGenericRepository<Dragon> DragonRepository { get; }
    IGenericRepository<Job> JobRepository { get; }
    IGenericRepository<Assignment> AssignmentRepository { get; }
    IGenericRepository<SkillTag> SkillTagRespository { get; }
    void Dispose();
    Task SaveAsync();

    // Many results
    IEnumerable<Assignment> GetOverlappingAssignments(int dragonId, DateTime periodStartUnix, DateTime periodEndUnix);
    IEnumerable<Dragon> GetDragonsWithoutOverlappingAssignments(int jobId, int[] skillTagIds, string? fightingSkill);
    IEnumerable<Dragon> GetAssignedDragons(int jobId);
    IEnumerable<JobWithCapacity> GetJobsWithCapacity(JobInclusions jobInclusions);
    IList<SkillTag> GetSkillTagsById(IList<int> skillTagIds);

    // 0 or 1 results
    Task<Dragon?> GetDragonWithJobAsync(int dragonId, JobInclusions jobInclusions);
    Task<Job?> GetJobWithSkillsAsync(int jobId);
    Task<Assignment?> GetAssignmentWithDragonAndJobAsync(int assignmentId);
    
    // boolean results
    Task<bool> DragonHasAnAssignment(int dragonId);
    Task<bool> JobHasAnAssignment(int jobId);
}
