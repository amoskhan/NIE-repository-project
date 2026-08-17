namespace Auth.Models;

public class IssuedLoginResponse
{
    public bool isAuthenticated { get; set; }
    public string? userId { get; set; }
    public string? userName { get; set; }
    public string? fullName { get; set; }
    public string? email { get; set; }
    public string? department { get; set; }
    public string? sessionToken { get; set; }
}
