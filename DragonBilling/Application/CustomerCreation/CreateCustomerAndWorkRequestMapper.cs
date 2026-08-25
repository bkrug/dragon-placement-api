using CSharpFunctionalExtensions;
using DragonBilling.Domain.Models;
using DragonCommon.Application;
using DragonCommon.Domain.Poco;

namespace DragonBilling.Application.CustomerCreation;

public static class CreateCustomerAndWorkRequestMapper
{
    public static Result<Customer, ValidationFailures> ToCustomer(CreateCustomerAndWorkRequest input)
    {
        var parsingFailures = TryParseDates(input, out var startDate, out var endDate);
        if (parsingFailures.FieldFailures.Count > 0)
            return Result.Failure<Customer, ValidationFailures>(parsingFailures);

        var customer = new Customer
        {
            Name = input.CustomerName,
            WorkRequests =
            [
                new WorkRequest
                {
                    Name = input.WorkRequestName,
                    Description = input.Description,
                    EstimatedStartDate = startDate,
                    EstimatedEndDate = endDate,
                    EstimatedWorkforceSize = input.EstimatedWorkforceSize
                }
            ]
        };
        return Result.Success<Customer, ValidationFailures>(customer);
    }

    private static ValidationFailures TryParseDates(
        CreateCustomerAndWorkRequest input, out DateTime? startDate, out DateTime? endDate)
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
