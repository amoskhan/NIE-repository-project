using System.Net;
using System.Text;
using System.Text.Json;

namespace Auth.Tests.TestDoubles;

internal static class IdentityProviderResponses
{
    public static Func<CapturedHttpRequest, HttpResponseMessage> Login(
        bool authenticated,
        string userId = "devia",
        string sessionToken = "idp-session-token",
        string? errorMessage = null)
    {
        var json = JsonSerializer.Serialize(new
        {
            isAuthenticated = authenticated,
            userId,
            fullName = "Dev IA",
            userName = "dev.ia",
            email = "dev.ia@nie.edu.sg",
            department = "Digital Solutions",
            sessionToken,
            errorMessage
        });

        return _ => JsonResponse(HttpStatusCode.OK, json);
    }

    public static Func<CapturedHttpRequest, HttpResponseMessage> Refresh(
        bool authenticated,
        string sessionToken = "rotated-session-token")
    {
        var json = JsonSerializer.Serialize(new
        {
            result = new { userId = "devia", authenticated, sessionToken }
        });

        return _ => JsonResponse(HttpStatusCode.OK, json);
    }

    public static Func<CapturedHttpRequest, HttpResponseMessage> VerifyUser(bool success)
    {
        var json = JsonSerializer.Serialize(new { success, status = success ? 200 : 401, userId = "devia" });
        return _ => JsonResponse(HttpStatusCode.OK, json);
    }

    public static Func<CapturedHttpRequest, HttpResponseMessage> Raw(string json) =>
        _ => JsonResponse(HttpStatusCode.OK, json);

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json) =>
        new(statusCode) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
}
