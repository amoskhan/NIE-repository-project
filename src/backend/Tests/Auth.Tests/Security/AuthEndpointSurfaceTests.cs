using System.Reflection;
using Auth.Controllers;
using Auth.Models;
using Auth.Services;
using Auth.Tests.TestDoubles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using NSubstitute;

namespace Auth.Tests.Security;

/// <summary>
/// NIE-AUTHN-004: anonymous endpoints must be marked explicitly and the Auth API must not become a
/// second source of browser-held credentials.
/// </summary>
public sealed class AuthEndpointSurfaceTests
{
    private static readonly Type ControllerType = typeof(AuthController);

    [Fact]
    public void The_auth_controller_declares_anonymous_access_explicitly()
    {
        Assert.NotNull(ControllerType.GetCustomAttribute<AllowAnonymousAttribute>(inherit: false));
        Assert.NotNull(ControllerType.GetCustomAttribute<ApiControllerAttribute>(inherit: false));
    }

    [Fact]
    public void No_auth_endpoint_claims_an_authorization_policy_it_cannot_enforce()
    {
        Assert.Null(ControllerType.GetCustomAttribute<AuthorizeAttribute>(inherit: true));

        var authorizedActions = PublicActions()
            .Where(action => action.GetCustomAttribute<AuthorizeAttribute>(inherit: true) is not null)
            .Select(action => action.Name)
            .ToArray();

        Assert.Empty(authorizedActions);
    }

    [Fact]
    public void Every_public_auth_action_declares_an_explicit_http_verb()
    {
        var actionsWithoutVerb = PublicActions()
            .Where(action => action.GetCustomAttributes<HttpMethodAttribute>(inherit: true).Any() is false)
            .Select(action => action.Name)
            .ToArray();

        Assert.Empty(actionsWithoutVerb);
    }

    [Fact]
    public void State_changing_auth_actions_are_not_reachable_over_get()
    {
        var stateChangingActions = new[] { "Login", "Logout", "Refresh", "CreateTestSession", "SsoCallback" };

        foreach (var actionName in stateChangingActions)
        {
            var action = Assert.Single(PublicActions(), candidate => candidate.Name == actionName);
            Assert.NotNull(action.GetCustomAttribute<HttpPostAttribute>(inherit: true));
            Assert.Null(action.GetCustomAttribute<HttpGetAttribute>(inherit: true));
        }
    }

    [Fact]
    public void The_auth_controller_is_routed_under_the_conventional_action_template()
    {
        var route = ControllerType.GetCustomAttribute<RouteAttribute>(inherit: false);

        Assert.NotNull(route);
        Assert.Equal("api/[controller]/[action]", route.Template);
    }

    [Fact]
    public async Task The_auth_api_never_issues_or_clears_a_browser_cookie()
    {
        var loginHarness = new AuthControllerHarness(identityProvider: IdentityProviderResponses.Login(true));
        loginHarness.SessionService
            .IssueSessionAsync(Arg.Any<LoginResponse>(), Arg.Any<CancellationToken>())
            .Returns(new IssuedLoginResponse { isAuthenticated = true, sessionToken = "issued-token" });
        await loginHarness.Controller.Login(new LoginRequest { userid = "devia", pd = "correct horse" });
        Assert.False(loginHarness.WroteAnyCookie);

        var testSessionHarness = new AuthControllerHarness("Development");
        await testSessionHarness.Controller.CreateTestSession(req: null);
        Assert.False(testSessionHarness.WroteAnyCookie);

        var logoutHarness = new AuthControllerHarness();
        logoutHarness.SeedSession("live-token", AuthControllerHarness.Session());
        await logoutHarness.Controller.Logout("live-token");
        Assert.False(logoutHarness.WroteAnyCookie);

        var refreshHarness = new AuthControllerHarness(identityProvider: IdentityProviderResponses.Refresh(true));
        refreshHarness.SeedSession("live-token", AuthControllerHarness.Session());
        await refreshHarness.Controller.Refresh("live-token");
        Assert.False(refreshHarness.WroteAnyCookie);

        var verifyHarness = new AuthControllerHarness();
        verifyHarness.SeedSession("live-token", AuthControllerHarness.Session());
        verifyHarness.UseSessionHeader("live-token");
        await verifyHarness.Controller.Verify();
        Assert.False(verifyHarness.WroteAnyCookie);
    }

    [Fact]
    public void Constructing_the_controller_without_an_identity_provider_subscription_key_fails_fast()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            _ = CreateControllerWithConfiguration(subscriptionKey: null, baseUrl: "https://idp.nie.edu.sg");
        });

        Assert.Contains("SubscriptionKey", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructing_the_controller_without_an_identity_provider_base_url_fails_fast()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            _ = CreateControllerWithConfiguration(subscriptionKey: "key", baseUrl: null);
        });

        Assert.Contains("BaseUrl", exception.Message, StringComparison.Ordinal);
    }

    private static AuthController CreateControllerWithConfiguration(string? subscriptionKey, string? baseUrl)
    {
        var cache = CacheSubstitute.Create();
        return new AuthController(
            HttpClientFactoryStub.Create(StubHttpMessageHandler.Ok("{}")),
            cache.Cache,
            Substitute.For<IWebHostEnvironment>(),
            ConfigurationStub.Create(
                ("NIEAuthApi:SubscriptionKey", subscriptionKey),
                ("NIEAuthApi:BaseUrl", baseUrl)),
            new RecordingLogger<AuthController>(),
            Substitute.For<IAuthSessionService>(),
            Substitute.For<IPortalSsoService>());
    }

    private static IEnumerable<MethodInfo> PublicActions() =>
        ControllerType
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName);
}
