using CommonDataLayer.Repositories;
using DragonAssignmentDomain.Enum;
using DragonAssignmentDomain.Models;
using DragonAssignmentDomain.Poco;

namespace DragonAssignmentApplication;

public interface IDragonPlacementUnitOfWork
{
    void Dispose();
    Task SaveAsync();

    // Dragon CRUD
    IEnumerable<Dragon> GetDragons();
    void InsertDragon(Dragon dragon);
    DeleteResult DeleteDragon(int dragonId);

    // Job CRUD
    Task<Job?> GetJobByIdAsync(int jobId);
    void InsertJob(Job job);
    DeleteResult DeleteJob(int jobId);

    // Assignment CRUD
    void InsertAssignment(Assignment assignment);
    IEnumerable<Assignment> GetAssignmentsByJobAndDragon(int jobId, int dragonId);
    void DeleteAssignment(Assignment assignment);

    // SkillTag queries
    IEnumerable<SkillTag> GetSkillTags();
    IList<SkillTag> GetSkillTagsById(IList<int> skillTagIds);

    // Complex queries
    IEnumerable<Assignment> GetOverlappingAssignments(int dragonId, DateTime periodStart, DateTime periodEnd);
    IEnumerable<Dragon> GetDragonsWithoutOverlappingAssignments(int jobId, int[] skillTagIds, string? fightingSkill);
    IEnumerable<Dragon> GetAssignedDragons(int jobId);
    IEnumerable<JobWithCapacity> GetJobsWithCapacity(JobInclusions jobInclusions);
    Task<IList<Dragon>> GetDragonWithJobAsync(int dragonId, JobInclusions jobInclusions);
    Task<IList<Job>> GetJobWithSkillsAsync(int jobId);
    Task<Assignment?> GetAssignmentWithDragonAndJobAsync(int assignmentId);
    Task<bool> DragonHasAnAssignment(int dragonId);
    Task<bool> JobHasAnAssignment(int jobId);
}
