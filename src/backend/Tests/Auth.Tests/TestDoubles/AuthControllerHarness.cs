using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Auth.Controllers;
using Auth.Models;
using Auth.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using NSubstitute;

namespace Auth.Tests.TestDoubles;

/// <summary>
/// Builds an <see cref="AuthController"/> wired to substituted collaborators and a
/// <see cref="DefaultHttpContext"/>, so controller branching can be exercised without starting a
/// web host.
/// </summary>
internal sealed class AuthControllerHarness
{
    public const string SessionKeyPrefix = "session:";
    public const string CallbackUrl = "https://auth.nie.edu.sg/api/Auth/SsoCallback";
    public const string IdentityProviderBaseUrl = "https://idp.nie.edu.sg";
    public const string SubscriptionKey = "idp-subscription-key";

    public AuthControllerHarness(
        string environmentName = "Production",
        Func<CapturedHttpRequest, HttpResponseMessage>? identityProvider = null,
        int sessionMinutes = 30)
    {
        HostEnvironment.EnvironmentName.Returns(environmentName);
        IdentityProvider = new StubHttpMessageHandler(
            identityProvider ?? (_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            }));

        HttpContext.Request.Scheme = "https";
        HttpContext.Request.Host = new HostString("auth.nie.edu.sg");

        UrlHelper.ActionContext.Returns(new ActionContext(HttpContext, new RouteData(), new ActionDescriptor()));
        UrlHelper.Action(Arg.Any<UrlActionContext>()).Returns(CallbackUrl);

        Controller = new AuthController(
            HttpClientFactoryStub.Create(IdentityProvider),
            Cache.Cache,
            HostEnvironment,
            ConfigurationStub.Create(
                ("NIEAuthApi:SubscriptionKey", SubscriptionKey),
                ("NIEAuthApi:BaseUrl", IdentityProviderBaseUrl),
                ("ValidSessionTimeInMins", sessionMinutes.ToString(CultureInfo.InvariantCulture))),
            Logger,
            SessionService,
            SsoService)
        {
            ControllerContext = new ControllerContext { HttpContext = HttpContext },
            Url = UrlHelper
        };
    }

    public CacheSubstitute Cache { get; } = CacheSubstitute.Create();

    public RecordingLogger<AuthController> Logger { get; } = new();

    public IAuthSessionService SessionService { get; } = Substitute.For<IAuthSessionService>();

    public IPortalSsoService SsoService { get; } = Substitute.For<IPortalSsoService>();

    public IWebHostEnvironment HostEnvironment { get; } = Substitute.For<IWebHostEnvironment>();

    public IUrlHelper UrlHelper { get; } = Substitute.For<IUrlHelper>();

    public DefaultHttpContext HttpContext { get; } = new();

    public StubHttpMessageHandler IdentityProvider { get; }

    public AuthController Controller { get; }

    public bool WroteAnyCookie => HttpContext.Response.Headers.ContainsKey("Set-Cookie");

    public void SeedSession(string sessionToken, AuthSessionDto session) =>
        Cache.WriteString(SessionKeyPrefix + sessionToken, JsonSerializer.Serialize(session));

    public void SeedRawSession(string sessionToken, string json) =>
        Cache.WriteString(SessionKeyPrefix + sessionToken, json);

    public bool HasSession(string sessionToken) => Cache.ContainsKey(SessionKeyPrefix + sessionToken);

    public AuthSessionDto? ReadSession(string sessionToken)
    {
        var json = Cache.ReadString(SessionKeyPrefix + sessionToken);
        return json is null ? null : JsonSerializer.Deserialize<AuthSessionDto>(json);
    }

    public void UseSessionHeader(string value) => HttpContext.Request.Headers["X-Session-Id"] = value;

    public void UseSessionQuery(string value) =>
        HttpContext.Request.QueryString = new QueryString("?sessionToken=" + Uri.EscapeDataString(value));

    public void UseCookies(params (string Name, string Value)[] cookies)
    {
        ArgumentNullException.ThrowIfNull(cookies);
        HttpContext.Request.Headers.Cookie =
            string.Join("; ", cookies.Select(cookie => cookie.Name + "=" + cookie.Value));
    }

    public void UseRemoteIp(string address) =>
        HttpContext.Connection.RemoteIpAddress = IPAddress.Parse(address);

    public static AuthSessionDto Session(string userId = "devia") => new()
    {
        UserId = userId,
        Name = "Dev IA",
        Email = "dev.ia@nie.edu.sg",
        Department = "Digital Solutions",
        LastActive = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Unspecified)
    };
}

internal static class ResponseValues
{
    public static IReadOnlyDictionary<string, object?> Of(object? value)
    {
        Assert.NotNull(value);
        return value.GetType()
            .GetProperties()
            .ToDictionary(property => property.Name, property => property.GetValue(value), StringComparer.Ordinal);
    }
}
