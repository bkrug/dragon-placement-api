using DragonBilling.Application;
using DragonBilling.Domain.Enum;
using DragonBilling.Domain.Models;
using DragonPlacementApi.Poco;
using DragonTimekeeping.Application;
using DragonTimekeeping.Domain.Enums;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace DragonPlacementApi.Endpoints;

public class BillingEndpoints
{
    public static async Task<Results<Ok<ValidatedResponse>, BadRequest<ValidatedResponse>>>
        BuildBillableHoursCandidatesAsync(
            IBillingUnitOfWork unitOfWork,
            ITimekeepingUnitOfWork timekeepingUnitOfWork,
            [FromQuery(Name = "startDate")] string startDateString,
            [FromQuery(Name = "endDate")] string endDateString
        )
    {
        if (!DateTime.TryParse(startDateString, out var startDate))
            return TypedResults.BadRequest(new ValidatedResponse { ValidationFailures = ["startDate must be an ISO Date"] });
        if (!DateTime.TryParse(endDateString, out var endDate))
            return TypedResults.BadRequest(new ValidatedResponse { ValidationFailures = ["endDate must be an ISO Date"] });

        var submittedPayPeriods = timekeepingUnitOfWork.PayPeriodRepository
            .Get(filter: pp => pp.StartDate == startDate && pp.EndDate == endDate)
            .Where(pp => pp.SubmissionStatus == PayPeriodStatus.Submitted)
            .ToList();

        var assignmentIds = submittedPayPeriods.Select(pp => pp.AssignmentId).ToHashSet();
        var chargeRatesByAssignment = unitOfWork.ChargeRateRepository
            .Get(filter: cr => assignmentIds.Contains(cr.AssignmentId))
            .ToDictionary(cr => cr.AssignmentId);

        foreach (var payPeriod in submittedPayPeriods)
        {
            if (!chargeRatesByAssignment.TryGetValue(payPeriod.AssignmentId, out var chargeRate))
                continue;

            var totalHours = (decimal)payPeriod.HoursWorked
                .Sum(hw => (hw.EndDateTime - hw.StartDateTime).TotalHours);

            unitOfWork.BillableHoursRepository.Insert(new BillableHours
            {
                ChargeRateId = chargeRate.ChargeRateId,
                PayPeriodId = payPeriod.PayPeriodId,
                HourlyRate = chargeRate.HourlyRate,
                TotalHours = totalHours,
                Status = BillingStatus.Draft
            });
        }

        await unitOfWork.SaveAsync();

        return TypedResults.Ok(ValidatedResponse.Success);
    }
}