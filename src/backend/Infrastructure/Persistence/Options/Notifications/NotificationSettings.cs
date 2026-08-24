namespace Infrastructure.Options;

public sealed class NotificationSettings
{
    public int RetentionDays { get; set; } = 183;
    public int MaxDeliveryAttempts { get; set; } = 5;
    public int DispatchBatchSize { get; set; } = 50;
}
