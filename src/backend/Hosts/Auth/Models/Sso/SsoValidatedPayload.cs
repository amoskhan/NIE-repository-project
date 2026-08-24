namespace Auth.Models;

public class SsoValidatedPayload
{
    public string State { get; set; } = default!;
    public string Jti { get; set; } = default!;
    public string SourceSystemId { get; set; } = default!;
    public string? SourceUrl { get; set; }
    public string ExchangeToken { get; set; } = default!;
    public string? Email { get; set; }
    public string? UserName { get; set; }
    public string? Subject { get; set; }
}
