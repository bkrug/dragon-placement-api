using CSharpFunctionalExtensions;
using DragonAssignmentDomain.Poco;
using DragonCommonDomain;

namespace DragonAssignmentDomain.Models;

public class DragonValidation
{
    public static Result<Dragon, DragonValidationFailures> Validate(Dragon dragon)
    {
        Dictionary<string, string> failures =
            new List<(string, string)>()
            {
                ( nameof(Dragon.GivenName), ValidateGivenName(dragon.GivenName) ),
                ( nameof(Dragon.WeightInKg), ValidatePositiveNumber(dragon.WeightInKg) ),
                ( nameof(Dragon.LengthInMeters), ValidatePositiveNumber(dragon.LengthInMeters) ),
                ( nameof(Dragon.FightingSkills), ValidateFightingSkills(dragon.FightingSkills) )
            }
            .Where(tuple => tuple.Item2 != string.Empty)
            .ToDictionary(tuple => tuple.Item1, tuple => tuple.Item2);

        if (failures.Count == 0) {
            return Result.Success<Dragon, DragonValidationFailures>(dragon);
        }
        else {
            var validationFailures = new DragonValidationFailures
            {
                GivenName = failures.GetValueOrDefault(nameof(Dragon.GivenName), null!),
                WeightInKg = failures.GetValueOrDefault(nameof(Dragon.WeightInKg), null!),
                LengthInMeters = failures.GetValueOrDefault(nameof(Dragon.LengthInMeters), null!),
                FightingSkills = failures.GetValueOrDefault(nameof(Dragon.FightingSkills), null!)
            };
            return Result.Failure<Dragon, DragonValidationFailures>(validationFailures);
        }
    }

    private static string ValidateGivenName(string givenName)
    {
        if (string.IsNullOrWhiteSpace(givenName))
            return ValidationMessages.IS_REQUIRED;
        return string.Empty;
    }

    private static string ValidatePositiveNumber(int? value)
    {
        if (value <= 0)
            return ValidationMessages.MUST_BE_A_POSITIVE_NUMBER;
        return string.Empty;
    }

    private static string ValidateFightingSkills(string? fightingSkills)
    {
        if (fightingSkills != null && fightingSkills is not ("b" or "m" or "a"))
            return "must be 'b', 'm', or 'a'";
        return string.Empty;
    }
}
