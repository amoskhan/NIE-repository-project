namespace Auth.Models;

public class VerifyResponse
{
    public bool success { get; set; }
    public int status { get; set; }
    public string? userId { get; set; }
    public string? ErrorMessage { get; set; }
}
