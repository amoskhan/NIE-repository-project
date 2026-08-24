using BuildingBlocks.Helpers;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Persistence;

public sealed class UnspecifiedDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UnspecifiedDateTimeConverter()
        : base(
            value => DateTimeHelper.AsUnspecified(value),
            value => DateTimeHelper.AsUnspecified(value))
    {
    }
}

public sealed class NullableUnspecifiedDateTimeConverter : ValueConverter<DateTime?, DateTime?>
{
    public NullableUnspecifiedDateTimeConverter()
        : base(
            value => DateTimeHelper.AsUnspecified(value),
            value => DateTimeHelper.AsUnspecified(value))
    {
    }
}
