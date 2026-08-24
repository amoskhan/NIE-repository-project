namespace Auth.Models;

public class SsoStateRecord
{
    public string State { get; set; } = default!;
    public string Nonce { get; set; } = default!;
    public string? ReturnUrl { get; set; }
    public string Status { get; set; } = SsoStateStatus.Pending;
    public string? ErrorMessage { get; set; }
    public IssuedLoginResponse? Login { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public static class SsoStateStatus
{
    public const string Pending = "pending";
    public const string Completed = "completed";
    public const string Failed = "failed";
}
