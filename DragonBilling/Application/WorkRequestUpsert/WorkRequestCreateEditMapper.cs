using CSharpFunctionalExtensions;
using DragonBilling.Domain.Models;
using DragonCommon.Application;
using DragonCommon.Domain.Poco;

namespace DragonBilling.Application.WorkRequestUpsert;

public static class WorkRequestCreateEditMapper
{
    public static Result<WorkRequest, ValidationFailures> ToWorkRequest(WorkRequestCreateEdit input, int customerId)
    {
        var parsingFailures = TryParseDates(input, out var startDate, out var endDate);
        if (parsingFailures.FieldFailures.Count > 0)
            return Result.Failure<WorkRequest, ValidationFailures>(parsingFailures);

        var workRequest = new WorkRequest
        {
            CustomerId = customerId,
            Name = input.Name,
            Description = input.Description,
            EstimatedStartDate = startDate,
            EstimatedEndDate = endDate,
            EstimatedWorkforceSize = input.EstimatedWorkforceSize
        };
        return Result.Success<WorkRequest, ValidationFailures>(workRequest);
    }

    private static ValidationFailures TryParseDates(
        WorkRequestCreateEdit input, out DateTime? startDate, out DateTime? endDate)
    {
        var fieldFailures = new Dictionary<string, string>();

        if (DateTime.TryParse(input.EstimatedStartDate, out var parsedStartDate))
            startDate = parsedStartDate;
        else
        {
            startDate = null;
            fieldFailures[nameof(WorkRequest.EstimatedStartDate)] = MappingMessages.MUST_BE_AN_ISO_DATE;
        }

        if (DateTime.TryParse(input.EstimatedEndDate, out var parsedEndDate))
            endDate = parsedEndDate;
        else
        {
            endDate = null;
            fieldFailures[nameof(WorkRequest.EstimatedEndDate)] = MappingMessages.MUST_BE_AN_ISO_DATE;
        }

        return new ValidationFailures { FieldFailures = fieldFailures };
    }
}
