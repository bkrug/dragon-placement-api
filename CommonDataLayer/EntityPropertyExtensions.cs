using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommonDataLayer;

public static class EntityPropertyExtensions
{
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
