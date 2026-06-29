using CSharpFunctionalExtensions;
using DragonAssignmentDomain.Models;
using DragonCommonDomain.Poco;

namespace DragonAssignmentApplication.DragonUpsert;

public static class DragonCreateEditMapper
{
    public static Result<Dragon, ValidationFailures> ToDragon(DragonCreateEdit input, IList<SkillTag> skillTags)
    {
        var dragon = new Dragon
        {
            GivenName = input.GivenName,
            FamilyName = input.FamilyName,
            WeightInKg = input.WeightInKg,
            LengthInMeters = input.LengthInMeters,
            FightingSkills = input.FightingSkills,
            SkillTags = skillTags
        };
        return Result.Success<Dragon, ValidationFailures>(dragon);
    }

    public static Result<Dragon, ValidationFailures> ApplyTo(DragonCreateEdit input, Dragon existing, IList<SkillTag> skillTags)
    {
        existing.GivenName = input.GivenName;
        existing.FamilyName = input.FamilyName;
        existing.WeightInKg = input.WeightInKg;
        existing.LengthInMeters = input.LengthInMeters;
        existing.FightingSkills = input.FightingSkills;
        existing.SkillTags = skillTags;
        return Result.Success<Dragon, ValidationFailures>(existing);
    }
}
