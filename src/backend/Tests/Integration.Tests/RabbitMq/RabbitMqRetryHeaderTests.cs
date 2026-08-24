using Infrastructure.Integration.RabbitMq;

namespace Integration.Tests.RabbitMq;

public sealed class RabbitMqRetryHeaderTests
{
    [Theory]
    [InlineData(null, 0)]
    [InlineData(0, 0)]
    [InlineData(2, 2)]
    [InlineData(3L, 3)]
    [InlineData("4", 4)]
    [InlineData(-1, 0)]
    public void ReadCompletedRetries_is_bounded(object? value, int expected)
    {
        var headers = value is null
            ? null
            : new Dictionary<string, object?> { [RabbitMqRetryHeader.Name] = value };

        Assert.Equal(expected, RabbitMqRetryHeader.ReadCompletedRetries(headers));
    }
}
