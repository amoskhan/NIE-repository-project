namespace Auth.Models;

public class LoginResponse
{
    public string? userId { get; set; }
    public string? firstName { get; set; }
    public string? lastName { get; set; }
    public string? fullName { get; set; }
    public string? userName { get; set; }
    public string? department { get; set; }
    public string? email { get; set; }
    public string? sessionToken { get; set; }
    public string? userType { get; set; }
    public bool isAuthenticated { get; set; }
    public string? errorMessage { get; set; }
}
