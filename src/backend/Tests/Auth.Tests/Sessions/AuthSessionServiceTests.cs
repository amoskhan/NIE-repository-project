using System.Globalization;
using System.Text.Json;
using Auth.Models;
using Auth.Services;
using Auth.Tests.TestDoubles;
using Microsoft.Extensions.Caching.Distributed;
using NSubstitute;

namespace Auth.Tests.Sessions;

/// <summary>
/// NIE-AUTHN-002: sessions must be opaque, server-side and expiring. These tests pin the session
/// creation contract of <see cref="AuthSessionService"/>, which is the only place a login result is
/// turned into a session.
/// </summary>
public sealed class AuthSessionServiceTests
{
    private const string SessionKeyPrefix = "session:";

    [Fact]
    public async Task Issuing_a_session_for_an_unauthenticated_login_response_is_refused()
    {
        var cache = CacheSubstitute.Create();
        var service = CreateService(cache, sessionMinutes: 30);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.IssueSessionAsync(
                new LoginResponse { isAuthenticated = false, userId = "devia", sessionToken = "idp-token" },
                TestContext.Current.CancellationToken));

        Assert.Contains("unauthenticated", exception.Message, StringComparison.Ordinal);
        Assert.Empty(cache.Keys);
        await cache.Cache.DidNotReceive().SetAsync(
            Arg.Any<string>(),
            Arg.Any<byte[]>(),
            Arg.Any<DistributedCacheEntryOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Issuing_a_session_stores_an_identity_only_record_under_the_session_key()
    {
        var cache = CacheSubstitute.Create();
        var service = CreateService(cache, sessionMinutes: 30);

        var issued = await service.IssueSessionAsync(
            new LoginResponse
            {
                isAuthenticated = true,
                sessionToken = "idp-token",
                userId = "devia",
                fullName = "Dev IA",
                userName = "dev.ia",
                email = "dev.ia@nie.edu.sg",
                department = "Digital Solutions"
            },
            TestContext.Current.CancellationToken);

        var stored = ReadSession(cache, issued.sessionToken!);
        Assert.Equal("devia", stored.UserId);
        Assert.Equal("Dev IA", stored.Name);
        Assert.Equal("dev.ia@nie.edu.sg", stored.Email);
        Assert.Equal("Digital Solutions", stored.Department);
        Assert.Equal([SessionKeyPrefix + "idp-token"], cache.Keys);
    }

    [Fact]
    public async Task Issued_session_records_carry_no_roles_permissions_or_credentials()
    {
        var cache = CacheSubstitute.Create();
        var service = CreateService(cache, sessionMinutes: 30);

        var issued = await service.IssueSessionAsync(
            AuthenticatedLogin("idp-token"),
            TestContext.Current.CancellationToken);

        var json = cache.ReadString(SessionKeyPrefix + issued.sessionToken)!;
        using var document = JsonDocument.Parse(json);
        var propertyNames = document.RootElement
            .EnumerateObject()
            .Select(property => property.Name)
            .ToArray();

        Assert.Equal(
            ["UserId", "LastActive", "Name", "Email", "Department"],
            propertyNames);
    }

    [Fact]
    public async Task Issuing_a_session_reuses_the_identity_provider_token_when_one_was_returned()
    {
        var cache = CacheSubstitute.Create();
        var service = CreateService(cache, sessionMinutes: 30);

        var issued = await service.IssueSessionAsync(
            AuthenticatedLogin("idp-token"),
            TestContext.Current.CancellationToken);

        Assert.Equal("idp-token", issued.sessionToken);
        Assert.True(cache.ContainsKey(SessionKeyPrefix + "idp-token"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public async Task Issuing_a_session_mints_an_opaque_token_when_the_identity_provider_supplied_none(
        string? identityProviderToken)
    {
        var cache = CacheSubstitute.Create();
        var service = CreateService(cache, sessionMinutes: 30);

        var issued = await service.IssueSessionAsync(
            AuthenticatedLogin(identityProviderToken),
            TestContext.Current.CancellationToken);

        var token = Assert.IsType<string>(issued.sessionToken);
        Assert.Equal(32, token.Length);
        Assert.True(token.All(Uri.IsHexDigit));
        Assert.True(cache.ContainsKey(SessionKeyPrefix + token));
    }

    [Fact]
    public async Task Successive_logins_never_share_a_minted_session_token()
    {
        var cache = CacheSubstitute.Create();
        var service = CreateService(cache, sessionMinutes: 30);

        var first = await service.IssueSessionAsync(
            AuthenticatedLogin(null),
            TestContext.Current.CancellationToken);
        var second = await service.IssueSessionAsync(
            AuthenticatedLogin(null),
            TestContext.Current.CancellationToken);

        Assert.NotEqual(first.sessionToken, second.sessionToken);
        Assert.Equal(2, cache.Keys.Count);
    }

    [Fact]
    public async Task A_minted_session_token_is_not_derived_from_the_user_identifier()
    {
        var cache = CacheSubstitute.Create();
        var service = CreateService(cache, sessionMinutes: 30);

        var issued = await service.IssueSessionAsync(
            new LoginResponse { isAuthenticated = true, userId = "devia", email = "dev.ia@nie.edu.sg" },
            TestContext.Current.CancellationToken);

        Assert.DoesNotContain("devia", issued.sessionToken!, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("dev.ia", issued.sessionToken!, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(30)]
    [InlineData(1)]
    [InlineData(1440)]
    public async Task Issuing_a_session_applies_the_configured_absolute_expiry(int minutes)
    {
        var cache = CacheSubstitute.Create();
        var service = CreateService(cache, minutes);

        var issued = await service.IssueSessionAsync(
            AuthenticatedLogin("idp-token"),
            TestContext.Current.CancellationToken);

        var options = cache.OptionsFor(SessionKeyPrefix + issued.sessionToken);
        Assert.NotNull(options);
        Assert.Equal(TimeSpan.FromMinutes(minutes), options.AbsoluteExpirationRelativeToNow);
        Assert.Null(options.SlidingExpiration);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("0")]
    [InlineData("-1")]
    public void Absent_or_non_positive_session_lifetime_configuration_breaks_the_service(string? configuredMinutes)
    {
        var cache = CacheSubstitute.Create();
        var configuration = ConfigurationStub.Create(("ValidSessionTimeInMins", configuredMinutes));

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            _ = new AuthSessionService(cache.Cache, configuration);
        });

        Assert.Equal("AbsoluteExpirationRelativeToNow", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-number")]
    [InlineData("30 minutes")]
    public void Non_numeric_session_lifetime_configuration_breaks_the_service(string configuredMinutes)
    {
        var cache = CacheSubstitute.Create();
        var configuration = ConfigurationStub.Create(("ValidSessionTimeInMins", configuredMinutes));

        Assert.Throws<FormatException>(() =>
        {
            _ = new AuthSessionService(cache.Cache, configuration);
        });
    }

    [Theory]
    [InlineData("Dev IA", "dev.ia", "Dev IA")]
    [InlineData(null, "dev.ia", "dev.ia")]
    [InlineData(null, null, "")]
    [InlineData("", "dev.ia", "")]
    public async Task Session_display_name_falls_back_from_full_name_to_user_name(
        string? fullName,
        string? userName,
        string expectedName)
    {
        var cache = CacheSubstitute.Create();
        var service = CreateService(cache, sessionMinutes: 30);

        var issued = await service.IssueSessionAsync(
            new LoginResponse
            {
                isAuthenticated = true,
                sessionToken = "idp-token",
                userId = "devia",
                fullName = fullName,
                userName = userName
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(expectedName, ReadSession(cache, issued.sessionToken!).Name);
    }

    [Theory]
    [InlineData("dev.ia", "Dev IA", "dev.ia")]
    [InlineData(null, "Dev IA", "Dev IA")]
    [InlineData(null, null, null)]
    public async Task Issued_response_user_name_falls_back_to_the_full_name(
        string? userName,
        string? fullName,
        string? expectedUserName)
    {
        var cache = CacheSubstitute.Create();
        var service = CreateService(cache, sessionMinutes: 30);

        var issued = await service.IssueSessionAsync(
            new LoginResponse
            {
                isAuthenticated = true,
                sessionToken = "idp-token",
                userId = "devia",
                userName = userName,
                fullName = fullName
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(expectedUserName, issued.userName);
    }

    [Fact]
    public async Task Null_identity_fields_are_normalised_to_empty_strings_in_the_stored_session()
    {
        var cache = CacheSubstitute.Create();
        var service = CreateService(cache, sessionMinutes: 30);

        var issued = await service.IssueSessionAsync(
            new LoginResponse { isAuthenticated = true, sessionToken = "idp-token" },
            TestContext.Current.CancellationToken);

        var stored = ReadSession(cache, issued.sessionToken!);
        Assert.Equal(string.Empty, stored.UserId);
        Assert.Equal(string.Empty, stored.Name);
        Assert.Equal(string.Empty, stored.Email);
        Assert.Equal(string.Empty, stored.Department);
    }

    [Fact]
    public async Task Issuing_a_session_stamps_the_last_active_time()
    {
        var cache = CacheSubstitute.Create();
        var service = CreateService(cache, sessionMinutes: 30);
        var before = DateTime.Now.AddSeconds(-5);

        var issued = await service.IssueSessionAsync(
            AuthenticatedLogin("idp-token"),
            TestContext.Current.CancellationToken);

        var lastActive = ReadSession(cache, issued.sessionToken!).LastActive;
        Assert.InRange(lastActive, before, DateTime.Now.AddSeconds(5));
    }

    [Fact]
    public async Task Issuing_a_session_forwards_the_caller_cancellation_token_to_the_cache()
    {
        var cache = CacheSubstitute.Create();
        var service = CreateService(cache, sessionMinutes: 30);
        var cancellationToken = TestContext.Current.CancellationToken;

        await service.IssueSessionAsync(AuthenticatedLogin("idp-token"), cancellationToken);

        await cache.Cache.Received(1).SetAsync(
            SessionKeyPrefix + "idp-token",
            Arg.Any<byte[]>(),
            Arg.Any<DistributedCacheEntryOptions>(),
            cancellationToken);
    }

    [Fact]
    public async Task Issued_response_reports_authentication_and_echoes_the_identity()
    {
        var cache = CacheSubstitute.Create();
        var service = CreateService(cache, sessionMinutes: 30);

        var issued = await service.IssueSessionAsync(
            new LoginResponse
            {
                isAuthenticated = true,
                sessionToken = "idp-token",
                userId = "devia",
                fullName = "Dev IA",
                userName = "dev.ia",
                email = "dev.ia@nie.edu.sg",
                department = "Digital Solutions"
            },
            TestContext.Current.CancellationToken);

        Assert.True(issued.isAuthenticated);
        Assert.Equal("devia", issued.userId);
        Assert.Equal("Dev IA", issued.fullName);
        Assert.Equal("dev.ia@nie.edu.sg", issued.email);
        Assert.Equal("Digital Solutions", issued.department);
    }

    private static AuthSessionService CreateService(CacheSubstitute cache, int sessionMinutes) =>
        new(
            cache.Cache,
            ConfigurationStub.Create(
                ("ValidSessionTimeInMins", sessionMinutes.ToString(CultureInfo.InvariantCulture))));

    private static LoginResponse AuthenticatedLogin(string? sessionToken) =>
        new()
        {
            isAuthenticated = true,
            sessionToken = sessionToken,
            userId = "devia",
            fullName = "Dev IA",
            email = "dev.ia@nie.edu.sg",
            department = "Digital Solutions"
        };

    private static AuthSessionDto ReadSession(CacheSubstitute cache, string sessionToken)
    {
        var json = cache.ReadString(SessionKeyPrefix + sessionToken);
        Assert.NotNull(json);
        var session = JsonSerializer.Deserialize<AuthSessionDto>(json);
        Assert.NotNull(session);
        return session;
    }
}
