namespace Auth.Models;

public class CreateTestSessionRequest
{
    public string? UserId { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Department { get; set; }
}
