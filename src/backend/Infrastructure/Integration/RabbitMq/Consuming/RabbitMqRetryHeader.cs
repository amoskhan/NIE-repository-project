using System.Globalization;
using System.Text;

namespace Infrastructure.Integration.RabbitMq;

public static class RabbitMqRetryHeader
{
    public const string Name = "x-nie-retry-count";

    public static int ReadCompletedRetries(IDictionary<string, object?>? headers)
    {
        if (headers is null || !headers.TryGetValue(Name, out var value) || value is null)
        {
            return 0;
        }

        var retries = value switch
        {
            byte typed => typed,
            short typed => typed,
            int typed => typed,
            long typed when typed <= int.MaxValue => (int)typed,
            byte[] bytes when int.TryParse(Encoding.UTF8.GetString(bytes), CultureInfo.InvariantCulture, out var parsed) => parsed,
            string text when int.TryParse(text, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => 0,
        };

        return Math.Max(0, retries);
    }
}
