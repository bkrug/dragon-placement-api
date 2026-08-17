using DragonCommonApplication;
using DragonTimekeeping.Domain.Models;

namespace DragonTimekeeping.Application.SinglePayPeriodQuery;

public static class PayPeriodViewMapper
{
    public static PayPeriodView ToView(PayPeriod entry, string dragonName, string assignmentDescription)
    {
        return new PayPeriodView
        {
            AssignmentId = entry.AssignmentId,
            StartDate = entry.StartDate.ToIsoDateString(),
            EndDate = entry.EndDate.ToIsoDateString(),
            SubmissionStatus = entry.SubmissionStatus,
            DragonName = dragonName,
            AssignmentDescription = assignmentDescription,
            HoursWorked = entry.HoursWorked.Select(hw => new HoursWorkedView
            {
                StartDateTime = hw.StartDateTime.ToIsoDateTimeString(),
                EndDateTime = hw.EndDateTime.ToIsoDateTimeString()
            }).ToList()
        };
    }
}
