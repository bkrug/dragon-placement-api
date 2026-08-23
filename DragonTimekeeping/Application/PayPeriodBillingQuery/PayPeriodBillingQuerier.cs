using CSharpFunctionalExtensions;
using DragonCommon.Domain;

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
            .Where(pp => pp.IsSubmitted)
            .Select(pp => new PayPeriodDataForBilling
            {
                PayPeriodId = pp.PayPeriodId,
                AssignmentId = pp.AssignmentId,
                TotalHoursWorked = pp.CalculateTotalHoursWorked()
            })
            .ToList();

        return Result.Success<List<PayPeriodDataForBilling>, string>(submittedPayPeriods);
    }
}
