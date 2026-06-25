using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DragonCommonDataLayer;

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
}
