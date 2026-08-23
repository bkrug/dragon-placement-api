using DragonBilling.Domain.Enum;
using DragonBilling.Domain.Models;
using DragonCommon.Domain;

namespace DragonBilling.Application.BillableHoursGeneration;

public static class BillableHoursGenerationService
{
    public static async Task GenerateBillableHoursAsync(
        IBillingUnitOfWork unitOfWork,
        List<PayPeriodDataForBilling> submittedPayPeriods)
    {
        var assignmentIds = submittedPayPeriods.Select(pp => pp.AssignmentId).Distinct();
        var chargeRatesByAssignment = unitOfWork.ChargeRateRepository
            .Get(filter: cr => assignmentIds.Contains(cr.AssignmentId))
            .ToDictionary(cr => cr.AssignmentId);

        foreach (var payPeriod in submittedPayPeriods)
        {
            if (!chargeRatesByAssignment.TryGetValue(payPeriod.AssignmentId, out var chargeRate))
                continue;

            unitOfWork.BillableHoursRepository.Insert(new BillableHours
            {
                ChargeRateId = chargeRate.ChargeRateId,
                PayPeriodId = payPeriod.PayPeriodId,
                HourlyRate = chargeRate.HourlyRate,
                TotalHours = payPeriod.TotalHoursWorked,
                Status = BillingStatus.Draft
            });
        }

        await unitOfWork.SaveAsync();
    }
}
