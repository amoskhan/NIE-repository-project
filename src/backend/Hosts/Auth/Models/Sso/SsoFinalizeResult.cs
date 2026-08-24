namespace Auth.Models;

public class SsoFinalizeResult
{
    public string status { get; set; } = SsoStateStatus.Pending;
    public int pollIntervalMs { get; set; }
    public string? message { get; set; }
    public IssuedLoginResponse? login { get; set; }
}
