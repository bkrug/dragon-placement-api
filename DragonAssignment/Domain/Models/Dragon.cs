using CSharpFunctionalExtensions;
using DragonCommon.Domain;
using DragonCommon.Domain.Poco;

namespace DragonAssignment.Domain.Models;

public partial class Dragon
{
    public int DragonId { get; set; }

    public string GivenName { get; set; } = null!;

    public string? FamilyName { get; set; }

    public int? WeightInKg { get; set; }

    public int? LengthInMeters { get; set; }

    public string? FightingSkills { get; set; }

    public virtual ICollection<Assignment> Assignments { get; set; } = [];

    public virtual ICollection<SkillTag> SkillTags { get; set; } = [];

    public Result<Dragon, ValidationFailures> Validate()
    {
        Dictionary<string, string> failures =
            new List<(string, string)>()
            {
                ( nameof(GivenName), ValidateGivenName() ),
                ( nameof(WeightInKg), ValidatePositiveNumber(WeightInKg) ),
                ( nameof(LengthInMeters), ValidatePositiveNumber(LengthInMeters) ),
                ( nameof(FightingSkills), ValidateFightingSkills() )
            }
            .Where(tuple => tuple.Item2 != string.Empty)
            .ToDictionary(tuple => tuple.Item1, tuple => tuple.Item2);

        return failures.Count == 0
            ? Result.Success<Dragon, ValidationFailures>(this)
            : Result.Failure<Dragon, ValidationFailures>(new ValidationFailures { FieldFailures = failures });
    }

    private string ValidateGivenName() =>
        string.IsNullOrWhiteSpace(GivenName)
        ? ValidationMessages.IS_REQUIRED
        : string.Empty;

    private static string ValidatePositiveNumber(int? value) =>
        value <= 0
        ? ValidationMessages.MUST_BE_A_POSITIVE_NUMBER
        : string.Empty;

    private string ValidateFightingSkills() =>
        FightingSkills != null && FightingSkills is not ("b" or "m" or "a")
        ? "must be 'b', 'm', or 'a'"
        : string.Empty;
}
