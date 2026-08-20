using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DragonCommon.Data;

public static class EntityPropertyExtensions
{
    /// <summary>
    /// Used for a domain model whose field is of type DateTime,
    /// but an SQLite database stores the value as a long integer.
    /// </summary>
    /// <param name="databaseFieldName">
    /// The name of the field as it appears in the database.
    /// Often the same as the domain's field name, but with the "Unix" suffix added.
    /// </param>
    public static void IsUnixSecondsType(this PropertyBuilder<DateTime> domainModelProp, string databaseFieldName)
    {
        domainModelProp
            .HasColumnName(databaseFieldName)
            .HasConversion(
                d => new DateTimeOffset(d, TimeSpan.Zero).ToUnixTimeSeconds(),
                unix => DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime
            );
    }

    /// <summary>
    /// Used for a domain model whose field is an enum,
    /// but the database stores the value as the enum member's name.
    /// </summary>
    /// <param name="databaseFieldName">
    /// The name of the field as it appears in the database.
    /// Often the same as the domain's field name.
    /// </param>
    public static void IsEnumNameType<TEnum>(this PropertyBuilder<TEnum> domainModelProp, string databaseFieldName)
        where TEnum : struct, Enum
    {
        _ = domainModelProp
            .HasColumnName(databaseFieldName)
            .HasConversion(
                e => e.ToString(),
                name => ParseEnumName<TEnum>(name)
            );
    }

    private static TEnum ParseEnumName<TEnum>(string name) where TEnum : struct, Enum
        => Enum.TryParse(name, ignoreCase: true, out TEnum parsedVal) ? parsedVal : default;
}
