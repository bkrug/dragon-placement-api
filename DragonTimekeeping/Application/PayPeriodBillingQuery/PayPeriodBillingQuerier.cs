using CSharpFunctionalExtensions;
using DragonCommon.Domain;
using DragonTimekeeping.Domain.Enums;

namespace DragonTimekeeping.Application.PayPeriodBillingQuery;

public static class PayPeriodBillingQuerier
{
    public static Result<List<PayPeriodDataForBilling>, string> GetSubmittedPayPeriodsForBilling(
        ITimekeepingUnitOfWork unitOfWork,
        string startDateString,
        string endDateString)
    {
        if (!DateOnly.TryParse(startDateString, out var startDateOnly))
            return Result.Failure<List<PayPeriodDataForBilling>, string>("startDate must be an ISO Date");
        if (!DateOnly.TryParse(endDateString, out var endDateOnly))
            return Result.Failure<List<PayPeriodDataForBilling>, string>("endDate must be an ISO Date");
        var startDate = startDateOnly.ToDateTime(TimeOnly.MinValue);
        var endDate = endDateOnly.ToDateTime(TimeOnly.MinValue);

        var submittedPayPeriods = unitOfWork.PayPeriodRepository
            .Get(filter: pp => pp.EndDate >= startDate && pp.EndDate <= endDate)
            .Where(pp => pp.SubmissionStatus == PayPeriodStatus.Submitted)
            .Select(pp => new PayPeriodDataForBilling
            {
                PayPeriodId = pp.PayPeriodId,
                AssignmentId = pp.AssignmentId,
                TotalHoursWorked = (decimal)pp.HoursWorked.Sum(hw => (hw.EndDateTime - hw.StartDateTime).TotalHours)
            })
            .ToList();

        return Result.Success<List<PayPeriodDataForBilling>, string>(submittedPayPeriods);
    }
}
