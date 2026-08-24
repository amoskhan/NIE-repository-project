using Application.Integration;

namespace Infrastructure.Integration.RabbitMq;

public enum RabbitMqDeliveryDecision
{
    Retry,
    DeadLetter,
}

public static class RabbitMqDeliveryPolicy
{
    public static RabbitMqDeliveryDecision Decide(
        Exception exception,
        int completedRetries,
        int maximumDeliveryAttempts)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentOutOfRangeException.ThrowIfNegative(completedRetries);
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumDeliveryAttempts, 1);

        if (exception is PermanentIntegrationEventException)
        {
            return RabbitMqDeliveryDecision.DeadLetter;
        }

        return completedRetries + 1 >= maximumDeliveryAttempts
            ? RabbitMqDeliveryDecision.DeadLetter
            : RabbitMqDeliveryDecision.Retry;
    }
}
