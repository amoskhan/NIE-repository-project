using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace Auth.Tests.TestDoubles;

internal static class ConfigurationStub
{
    public static IConfiguration Create(params (string Key, string? Value)[] settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var configuration = Substitute.For<IConfiguration>();
        foreach (var (key, value) in settings)
            configuration[key].Returns(value);

        return configuration;
    }
}
