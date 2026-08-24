namespace Application.Integration;

/// <summary>
/// Signals a payload or business failure that must not be retried.
/// </summary>
public sealed class PermanentIntegrationEventException : Exception
{
    public PermanentIntegrationEventException(string message)
        : base(message)
    {
    }

    public PermanentIntegrationEventException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
