using DragonCommonApplication;

namespace DragonTimekeeping.Application.PotentialPayPeriodQuery;

public static class PotentialPayPeriodQuerier
{
    /// <summary>
    /// Get all of the pay periods that the user is allowed to create new pay period records for.
    /// Do not allow the user to re-create an existing pay period for the current assignment.
    /// And a user can only go backwards so far in time if hours have not already been reported.
    /// </summary>
    public static List<ValidPaySpan> GetValidPayPeriods(ITimekeepingUnitOfWork unitOfWork, int assignmentId)
    {
        //The maximum number of weeks that the user is allowed to book at any given time.
        const int REPORTABLE_WEEKS = 4;

        var today = DateTime.UtcNow.Date;
        var daysToSubtract = ((int)today.DayOfWeek + 6) % 7;
        var monday = today.AddDays(-daysToSubtract);

        //TODO: Write a method that will limit the result set.
        // We don't need records going backwards more than REPORTABLE_WEEKS+1.
        var existingStarts = unitOfWork.PayPeriodRepository
            .Get(pp => pp.AssignmentId == assignmentId)
            .Select(pp => pp.StartDate)
            .ToHashSet();

        var candidates = Enumerable.Range(0, REPORTABLE_WEEKS)
            .Select(weeksAgo => monday.AddDays(-7 * weeksAgo))
            .Where(startDate => !existingStarts.Contains(startDate))
            .Select(startDate => new ValidPaySpan
            {
                StartDate = startDate.ToIsoDateString(),
                EndDate = startDate.AddDays(6).ToIsoDateString()
            })
            .ToList();

        return candidates;
    }

}