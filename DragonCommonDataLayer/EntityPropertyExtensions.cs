using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DragonCommonDataLayer;

public static class EntityPropertyExtensions
{
    /// <summary>
    /// Used for a domain model whose field is of type DateTime,
    /// but an SQLite database stores the value as a long integer.
    /// </summary>
    public static void IsUnixSecondsType(this PropertyBuilder<DateTime> dateTimeProp, string fieldName)
    {
        dateTimeProp
            .HasColumnName(fieldName)
            .HasConversion(
                d => new DateTimeOffset(d, TimeSpan.Zero).ToUnixTimeSeconds(),
                unix => DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime
            );
    }
}
