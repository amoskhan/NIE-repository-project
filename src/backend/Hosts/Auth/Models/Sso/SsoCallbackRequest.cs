namespace Auth.Models;

public class SsoCallbackRequest
{
    public required string state { get; set; }
    public required string encryptedPayload { get; set; }
}
