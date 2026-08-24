namespace Auth.Models;

public class SsoStartResponse
{
    public string state { get; set; } = default!;
    public string nonce { get; set; } = default!;
    public string launchUrl { get; set; } = default!;
    public int pollIntervalMs { get; set; }
}
