namespace Auth.Models;

public class CreateTestSessionResponse
{
    public bool Success { get; set; }
    public string? SessionToken { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? ErrorMessage { get; set; }
}
