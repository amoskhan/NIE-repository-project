namespace Infrastructure.Integration.RabbitMq;

public sealed class RabbitMqSubscriptionState
{
    private readonly object _sync = new();
    private bool _isReady;
    private string _status = "RabbitMQ subscriptions have not started.";

    public (bool IsReady, string Status) Snapshot()
    {
        lock (_sync)
        {
            return (_isReady, _status);
        }
    }

    public void MarkReady(int subscriptionCount)
    {
        lock (_sync)
        {
            _isReady = true;
            _status = $"{subscriptionCount} RabbitMQ subscription(s) are active.";
        }
    }

    public void MarkUnavailable(string status)
    {
        lock (_sync)
        {
            _isReady = false;
            _status = string.IsNullOrWhiteSpace(status)
                ? "RabbitMQ subscriptions are unavailable."
                : status;
        }
    }
}
