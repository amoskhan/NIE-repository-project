namespace Contracts.Integration;

public static class IntegrationContractRoutingKey
{
    public static string Create(string eventName, int eventVersion)
    {
        if (string.IsNullOrWhiteSpace(eventName))
        {
            throw new ArgumentException("The integration event name is required.", nameof(eventName));
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(eventVersion, 1);
        return $"{eventName}.v{eventVersion}";
    }
}
