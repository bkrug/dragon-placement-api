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
                ( "GivenName", ValidateGivenName(dragon.GivenName) ),
                ( "WeightInKg", ValidatePositiveNumber(dragon.WeightInKg) ),
                ( "LengthInMeters", ValidatePositiveNumber(dragon.LengthInMeters) ),
                ( "FightingSkills", ValidateFightingSkills(dragon.FightingSkills) )
            }
            .Where(tuple => tuple.Item2 != string.Empty)
            .ToDictionary(tuple => tuple.Item1, tuple => tuple.Item2);

        if (failures.Count == 0) {
            return Result.Success<Dragon, DragonValidationFailures>(dragon);
        }
        else {
            var validationFailures = new DragonValidationFailures
            {
                GivenName = failures.GetValueOrDefault("GivenName", null!),
                WeightInKg = failures.GetValueOrDefault("WeightInKg", null!),
                LengthInMeters = failures.GetValueOrDefault("LengthInMeters", null!),
                FightingSkills = failures.GetValueOrDefault("FightingSkills", null!)
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
