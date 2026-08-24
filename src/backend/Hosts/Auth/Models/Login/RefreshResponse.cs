namespace Auth.Models;

public class RefreshResponseRoot
{
    public RefreshResponse? result { get; set; }
}

public class RefreshResponse
{
    public string? userId { get; set; }
    public bool authenticated { get; set; }
    public string? sessionToken { get; set; }
}
