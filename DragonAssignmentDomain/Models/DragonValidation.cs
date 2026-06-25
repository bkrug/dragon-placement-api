using CSharpFunctionalExtensions;
using DragonAssignmentDomain.Poco;
using DragonCommonApplication;

namespace DragonAssignmentDomain.Models;

public class DragonValidation
{
    public static Result<Dragon, DragonValidationFailures> Validate(Dragon dragon)
    {
        var failures = new DragonValidationFailures();

        if (string.IsNullOrWhiteSpace(dragon.GivenName))
            failures.GivenName = ValidationMessages.IS_REQUIRED;
        if (dragon.WeightInKg <= 0)
            failures.WeightInKg = ValidationMessages.MUST_BE_A_POSITIVE_NUMBER;
        if (dragon.LengthInMeters <= 0)
            failures.LengthInMeters = ValidationMessages.MUST_BE_A_POSITIVE_NUMBER;
        if (dragon.FightingSkills != null && dragon.FightingSkills is not ("b" or "m" or "a"))
            failures.FightingSkills = "must be 'b', 'm', or 'a'";

        if (failures.GivenName != null || failures.WeightInKg != null
            || failures.LengthInMeters != null || failures.FightingSkills != null)
            return Result.Failure<Dragon, DragonValidationFailures>(failures);

        return Result.Success<Dragon, DragonValidationFailures>(dragon);
    }
}
