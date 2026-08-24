using System.Net;
using Auth.Models;
using Auth.Tests.TestDoubles;
using Jose;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;

namespace Auth.Tests.Sso;

/// <summary>
/// The Portal SSO callback is the only unauthenticated path that can mint a session from an
/// externally supplied token, so every rejection branch is exercised here against real nested
/// JWE/JWS payloads.
/// </summary>
public sealed class PortalSsoCallbackTests
{
    private const string State = "pending-state";
    private const string Nonce = "pending-nonce";

    [Fact]
    public async Task Handling_a_callback_while_the_feature_is_disabled_is_refused()
    {
        var harness = Pending(options => options.Enabled = false);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => harness.Service.HandleCallbackAsync(
                harness.Callback(),
                IPAddress.Loopback,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task A_callback_for_an_unknown_state_is_rejected_before_any_token_work()
    {
        var harness = new PortalSsoHarness(signingKeys: SsoSigningKeyPair.Create());

        var exception = await Assert.ThrowsAsync<SecurityTokenException>(
            () => harness.Service.HandleCallbackAsync(
                harness.Callback(),
                IPAddress.Loopback,
                TestContext.Current.CancellationToken));

        Assert.Equal("Unknown or expired SSO state.", exception.Message);
        Assert.Empty(harness.Exchange.Requests);
    }

    [Theory]
    [InlineData(SsoStateStatus.Completed)]
    [InlineData(SsoStateStatus.Failed)]
    public async Task A_callback_for_a_state_that_is_no_longer_pending_is_rejected(string status)
    {
        var harness = new PortalSsoHarness(signingKeys: SsoSigningKeyPair.Create());
        harness.SeedState(new SsoStateRecord { State = State, Nonce = Nonce, Status = status });

        var exception = await Assert.ThrowsAsync<SecurityTokenException>(
            () => harness.Service.HandleCallbackAsync(
                harness.Callback(),
                IPAddress.Loopback,
                TestContext.Current.CancellationToken));

        Assert.Equal("The SSO state is no longer pending.", exception.Message);
        Assert.Empty(harness.Exchange.Requests);
    }

    [Theory]
    [InlineData("10.1.2.0/24", "10.1.2.77", true)]
    [InlineData("10.1.2.0/24", "10.1.3.77", false)]
    [InlineData("10.0.0.0/8", "10.255.255.255", true)]
    [InlineData("10.0.0.0/8", "11.0.0.1", false)]
    [InlineData("0.0.0.0/0", "203.0.113.9", true)]
    [InlineData("10.1.2.7", "10.1.2.7", true)]
    [InlineData("10.1.2.7", "10.1.2.8", false)]
    [InlineData("::1", "::1", true)]
    [InlineData("10.1.2.0/24", "::1", false)]
    [InlineData("not-an-address/24", "10.1.2.7", false)]
    [InlineData("10.1.2.0/notanumber", "10.1.2.7", false)]
    [InlineData("   ", "10.1.2.7", false)]
    public async Task The_callback_source_allowlist_matches_exact_addresses_and_cidr_ranges(
        string allowedRange,
        string remoteAddress,
        bool expectedAllowed)
    {
        var harness = Pending(options => options.AllowedIpRanges = [allowedRange]);

        var exception = await Assert.ThrowsAsync<SecurityTokenException>(
            () => harness.Service.HandleCallbackAsync(
                new SsoCallbackRequest { state = State, encryptedPayload = "not-a-jwe" },
                IPAddress.Parse(remoteAddress),
                TestContext.Current.CancellationToken));

        // An allowlisted source gets as far as payload parsing; a blocked source never does.
        Assert.Equal(
            expectedAllowed ? "The SSO payload must be a JWE compact token." : "SSO callback source is not allowlisted.",
            exception.Message);
    }

    [Fact]
    public async Task An_empty_source_allowlist_accepts_every_caller()
    {
        var harness = Pending(options => options.AllowedIpRanges = []);

        var exception = await Assert.ThrowsAsync<SecurityTokenException>(
            () => harness.Service.HandleCallbackAsync(
                new SsoCallbackRequest { state = State, encryptedPayload = "not-a-jwe" },
                IPAddress.Parse("203.0.113.9"),
                TestContext.Current.CancellationToken));

        Assert.Equal("The SSO payload must be a JWE compact token.", exception.Message);
    }

    [Fact]
    public async Task A_caller_without_a_remote_address_bypasses_the_source_allowlist()
    {
        var harness = Pending(options => options.AllowedIpRanges = ["10.1.2.0/24"]);

        var exception = await Assert.ThrowsAsync<SecurityTokenException>(
            () => harness.Service.HandleCallbackAsync(
                new SsoCallbackRequest { state = State, encryptedPayload = "not-a-jwe" },
                remoteIp: null,
                TestContext.Current.CancellationToken));

        Assert.Equal("The SSO payload must be a JWE compact token.", exception.Message);
    }

    [Fact]
    public async Task A_blocked_source_marks_the_pending_state_as_failed()
    {
        var harness = Pending(options => options.AllowedIpRanges = ["10.1.2.0/24"]);

        await Assert.ThrowsAsync<SecurityTokenException>(
            () => harness.Service.HandleCallbackAsync(
                harness.Callback(),
                IPAddress.Parse("203.0.113.9"),
                TestContext.Current.CancellationToken));

        var record = harness.ReadState(State)!;
        Assert.Equal(SsoStateStatus.Failed, record.Status);
        Assert.Equal("SSO callback source is not allowlisted.", record.ErrorMessage);
        Assert.NotNull(record.CompletedAt);
    }

    [Theory]
    [InlineData("not-a-jwe")]
    [InlineData("a.b.c")]
    [InlineData("a.b.c.d")]
    [InlineData("a.b.c.d.e.f")]
    public async Task A_payload_that_is_not_a_five_part_jwe_is_rejected(string payload)
    {
        var harness = Pending();

        var exception = await Assert.ThrowsAsync<SecurityTokenException>(
            () => harness.Service.HandleCallbackAsync(
                new SsoCallbackRequest { state = State, encryptedPayload = payload },
                IPAddress.Loopback,
                TestContext.Current.CancellationToken));

        Assert.Equal("The SSO payload must be a JWE compact token.", exception.Message);
        Assert.Equal(SsoStateStatus.Failed, harness.ReadState(State)!.Status);
    }

    [Theory]
    [InlineData("RSA1_5")]
    [InlineData("dir")]
    [InlineData("none")]
    public async Task A_payload_encrypted_with_an_unsupported_key_algorithm_is_rejected(string alg)
    {
        var harness = Pending();

        var exception = await Assert.ThrowsAsync<SecurityTokenException>(
            () => harness.Service.HandleCallbackAsync(
                new SsoCallbackRequest
                {
                    state = State,
                    encryptedPayload = SsoTokenFactory.CompactTokenWithHeader(alg, "A256GCM")
                },
                IPAddress.Loopback,
                TestContext.Current.CancellationToken));

        Assert.Contains("unsupported JWE alg", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("A128CBC-HS256")]
    [InlineData("A128GCM")]
    public async Task A_payload_encrypted_with_an_unsupported_content_algorithm_is_rejected(string enc)
    {
        var harness = Pending();

        var exception = await Assert.ThrowsAsync<SecurityTokenException>(
            () => harness.Service.HandleCallbackAsync(
                new SsoCallbackRequest
                {
                    state = State,
                    encryptedPayload = SsoTokenFactory.CompactTokenWithHeader("RSA-OAEP-256", enc)
                },
                IPAddress.Loopback,
                TestContext.Current.CancellationToken));

        Assert.Contains("unsupported JWE enc", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_payload_that_cannot_be_decrypted_is_rejected()
    {
        var harness = Pending();

        var exception = await Assert.ThrowsAsync<SecurityTokenException>(
            () => harness.Service.HandleCallbackAsync(
                new SsoCallbackRequest { state = State, encryptedPayload = SsoTokenFactory.UndecryptableToken() },
                IPAddress.Loopback,
                TestContext.Current.CancellationToken));

        Assert.Equal("The SSO token could not be decrypted.", exception.Message);
    }

    [Fact]
    public async Task A_payload_cannot_be_processed_without_configured_key_material()
    {
        var harness = Pending(options =>
        {
            options.Crypto.DecryptionPrivateKeyPem = string.Empty;
            options.Crypto.DecryptionPrivateKeyPath = string.Empty;
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => harness.Service.HandleCallbackAsync(
                harness.Callback(),
                IPAddress.Loopback,
                TestContext.Current.CancellationToken));

        Assert.Contains("DecryptionPrivateKeyPem", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_payload_signed_by_an_untrusted_key_is_rejected()
    {
        var harness = Pending();

        var exception = await Assert.ThrowsAsync<SecurityTokenException>(
            () => harness.Service.HandleCallbackAsync(
                harness.Callback(signingPrivateKeyPem: SsoCryptoMaterial.UntrustedSigningPrivateKeyPem),
                IPAddress.Loopback,
                TestContext.Current.CancellationToken));

        Assert.Contains("Signature validation failed", exception.Message, StringComparison.Ordinal);
        Assert.Empty(harness.Exchange.Requests);
    }

    [Fact]
    public async Task A_payload_signed_with_a_weaker_algorithm_than_required_is_rejected()
    {
        var harness = Pending();

        var exception = await Assert.ThrowsAsync<SecurityTokenException>(
            () => harness.Service.HandleCallbackAsync(
                harness.Callback(signingAlgorithm: JwsAlgorithm.RS256),
                IPAddress.Loopback,
                TestContext.Current.CancellationToken));

        Assert.Contains("unsupported JWS alg 'RS256'", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_expired_payload_is_rejected()
    {
        var harness = Pending();
        var expired = harness.Callback(claims =>
        {
            claims["iat"] = DateTimeOffset.UtcNow.AddMinutes(-20).ToUnixTimeSeconds();
            claims["nbf"] = DateTimeOffset.UtcNow.AddMinutes(-20).ToUnixTimeSeconds();
            claims["exp"] = DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeSeconds();
        });

        await Assert.ThrowsAsync<SecurityTokenException>(
            () => harness.Service.HandleCallbackAsync(
                expired,
                IPAddress.Loopback,
                TestContext.Current.CancellationToken));

        Assert.Empty(harness.Exchange.Requests);
    }

    [Fact]
    public async Task A_payload_without_an_expiry_is_rejected()
    {
        var harness = Pending();

        await Assert.ThrowsAsync<SecurityTokenException>(
            () => harness.Service.HandleCallbackAsync(
                harness.Callback(claims => claims.Remove("exp")),
                IPAddress.Loopback,
                TestContext.Current.CancellationToken));

        Assert.Empty(harness.Exchange.Requests);
    }

    [Fact]
    public async Task A_payload_from_a_foreign_issuer_is_rejected()
    {
        var harness = Pending();

        await Assert.ThrowsAsync<SecurityTokenException>(
            () => harness.Service.HandleCallbackAsync(
                harness.Callback(claims => claims["iss"] = "https://evil.example.com"),
                IPAddress.Loopback,
                TestContext.Current.CancellationToken));

        Assert.Empty(harness.Exchange.Requests);
    }

    [Fact]
    public async Task A_payload_minted_for_another_audience_is_rejected()
    {
        var harness = Pending();

        await Assert.ThrowsAsync<SecurityTokenException>(
            () => harness.Service.HandleCallbackAsync(
                harness.Callback(claims => claims["aud"] = "some-other-application"),
                IPAddress.Loopback,
                TestContext.Current.CancellationToken));

        Assert.Empty(harness.Exchange.Requests);
    }

    [Fact]
    public async Task A_payload_whose_state_claim_does_not_match_the_request_is_rejected()
    {
        var harness = Pending();

        var exception = await Assert.ThrowsAsync<SecurityTokenException>(
            () => harness.Service.HandleCallbackAsync(
                harness.Callback(claims => claims["state"] = "some-other-state"),
                IPAddress.Loopback,
                TestContext.Current.CancellationToken));

        Assert.Equal("The SSO state claim does not match the requested state.", exception.Message);
    }

    [Fact]
    public async Task A_payload_whose_nonce_does_not_match_the_pending_login_is_rejected()
    {
        var harness = Pending();

        var exception = await Assert.ThrowsAsync<SecurityTokenException>(
            () => harness.Service.HandleCallbackAsync(
                harness.Callback(claims => claims["nonce"] = "replayed-nonce"),
                IPAddress.Loopback,
                TestContext.Current.CancellationToken));

        Assert.Equal("The SSO nonce claim does not match the pending login.", exception.Message);
    }

    [Fact]
    public async Task A_payload_from_an_untrusted_source_system_is_rejected()
    {
        var harness = Pending();

        var exception = await Assert.ThrowsAsync<SecurityTokenException>(
            () => harness.Service.HandleCallbackAsync(
                harness.Callback(claims => claims["source_system"] = "impostor"),
                IPAddress.Loopback,
                TestContext.Current.CancellationToken));

        Assert.Equal("The SSO payload source system is not trusted.", exception.Message);
    }

    [Theory]
    [InlineData("state")]
    [InlineData("nonce")]
    [InlineData("source_system")]
    [InlineData("exchange_token")]
    [InlineData("jti")]
    public async Task A_payload_missing_a_required_claim_is_rejected(string claimName)
    {
        var harness = Pending();

        var exception = await Assert.ThrowsAsync<SecurityTokenException>(
            () => harness.Service.HandleCallbackAsync(
                harness.Callback(claims => claims.Remove(claimName)),
                IPAddress.Loopback,
                TestContext.Current.CancellationToken));

        Assert.Equal($"Required SSO claim '{claimName}' is missing.", exception.Message);
    }

    [Theory]
    [InlineData("https://evil.example.com/launch")]
    [InlineData("//evil.example.com/launch")]
    [InlineData("https://portal.nie.edu.sg.evil.example.com/launch")]
    [InlineData("http://portal.nie.edu.sg/launch")]
    [InlineData("https://portal.nie.edu.sg/launch/../../admin")]
    [InlineData("https://portal.nie.edu.sg:8443/launch")]
    public async Task A_payload_whose_source_url_is_outside_the_allowlist_is_rejected(string sourceUrl)
    {
        var harness = Pending(options => options.AllowedSourceUrls = ["https://portal.nie.edu.sg/launch"]);

        var exception = await Assert.ThrowsAsync<SecurityTokenException>(
            () => harness.Service.HandleCallbackAsync(
                harness.Callback(claims => claims["source_url"] = sourceUrl),
                IPAddress.Loopback,
                TestContext.Current.CancellationToken));

        Assert.Equal("The SSO payload source URL is not trusted.", exception.Message);
    }

    [Theory]
    [InlineData("https://portal.nie.edu.sg/launch")]
    [InlineData("https://portal.nie.edu.sg/launch/")]
    [InlineData("https://PORTAL.NIE.edu.sg/launch")]
    [InlineData("https://portal.nie.edu.sg/launch?tenant=nie#top")]
    public async Task Source_url_matching_normalises_trailing_slashes_casing_query_and_fragment(string sourceUrl)
    {
        var harness = Pending(options => options.AllowedSourceUrls = ["https://portal.nie.edu.sg/launch/"]);

        var result = await harness.Service.HandleCallbackAsync(
            harness.Callback(claims => claims["source_url"] = sourceUrl),
            IPAddress.Loopback,
            TestContext.Current.CancellationToken);

        Assert.Equal(SsoStateStatus.Completed, result.status);
    }

    [Fact]
    public async Task A_payload_without_a_source_url_is_rejected_when_an_allowlist_is_configured()
    {
        var harness = Pending(options => options.AllowedSourceUrls = ["https://portal.nie.edu.sg/launch"]);

        var exception = await Assert.ThrowsAsync<SecurityTokenException>(
            () => harness.Service.HandleCallbackAsync(
                harness.Callback(claims => claims.Remove("source_url")),
                IPAddress.Loopback,
                TestContext.Current.CancellationToken));

        Assert.Equal("Required SSO claim 'source_url' is missing.", exception.Message);
    }

    [Fact]
    public async Task Without_a_source_url_allowlist_any_source_url_is_accepted()
    {
        var harness = Pending();

        var result = await harness.Service.HandleCallbackAsync(
            harness.Callback(claims => claims["source_url"] = "https://evil.example.com/launch"),
            IPAddress.Loopback,
            TestContext.Current.CancellationToken);

        // Documents the default posture: PortalSso:AllowedSourceUrls is empty out of the box.
        Assert.Equal(SsoStateStatus.Completed, result.status);
    }

    [Fact]
    public async Task A_successful_callback_issues_a_session_and_completes_the_state()
    {
        var harness = Pending();

        var result = await harness.Service.HandleCallbackAsync(
            harness.Callback(),
            IPAddress.Loopback,
            TestContext.Current.CancellationToken);

        Assert.Equal(SsoStateStatus.Completed, result.status);
        Assert.NotNull(result.login);
        Assert.Equal(PortalSsoHarness.IssuedSessionToken, result.login.sessionToken);

        var record = harness.ReadState(State)!;
        Assert.Equal(SsoStateStatus.Completed, record.Status);
        Assert.Equal(PortalSsoHarness.IssuedSessionToken, record.Login!.sessionToken);
        Assert.NotNull(record.CompletedAt);
        Assert.Null(record.ErrorMessage);
    }

    [Fact]
    public async Task A_successful_callback_delegates_session_creation_to_the_session_service()
    {
        var harness = Pending();

        await harness.Service.HandleCallbackAsync(
            harness.Callback(),
            IPAddress.Loopback,
            TestContext.Current.CancellationToken);

        await harness.SessionService.Received(1).IssueSessionAsync(
            Arg.Is<LoginResponse>(login => login != null && login.isAuthenticated && login.userId == "devia"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_successful_callback_forwards_the_validated_identity_to_the_exchange_api()
    {
        var harness = Pending();

        await harness.Service.HandleCallbackAsync(
            harness.Callback(),
            IPAddress.Loopback,
            TestContext.Current.CancellationToken);

        var request = Assert.Single(harness.Exchange.Requests);
        Assert.Equal("https://exchange.nie.edu.sg/sso/exchange", request.Url);
        Assert.Equal("sso-subscription-key", request.Headers["x-sso-api-key"]);
        Assert.Equal("portal", request.Headers["X-Source-System"]);
        Assert.Contains(
            "\"exchangeToken\":\"" + SsoTokenFactory.ExchangeToken + "\"",
            request.Body,
            StringComparison.Ordinal);
        Assert.Contains("\"state\":\"" + State + "\"", request.Body, StringComparison.Ordinal);
        Assert.Contains("\"username\":\"dev.ia\"", request.Body, StringComparison.Ordinal);
        Assert.Contains("\"subject\":\"devia\"", request.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_payload_whose_identifier_was_already_consumed_is_rejected()
    {
        var harness = Pending();
        harness.SeedReplayMarker("already-used-payload-id");

        var exception = await Assert.ThrowsAsync<SecurityTokenException>(
            () => harness.Service.HandleCallbackAsync(
                harness.Callback(claims => claims["jti"] = "already-used-payload-id"),
                IPAddress.Loopback,
                TestContext.Current.CancellationToken));

        Assert.Equal("The SSO payload has already been used.", exception.Message);
        Assert.Equal(SsoStateStatus.Failed, harness.ReadState(State)!.Status);
        Assert.Empty(harness.Exchange.Requests);
    }

    [Fact]
    public async Task A_consumed_payload_id_is_remembered_for_the_configured_replay_window()
    {
        var harness = Pending(options => options.ReplayTtlMinutes = 11);

        await harness.Service.HandleCallbackAsync(
            harness.Callback(claims => claims["jti"] = "payload-id"),
            IPAddress.Loopback,
            TestContext.Current.CancellationToken);

        Assert.True(harness.HasReplayMarker("payload-id"));
        var options = harness.Cache.OptionsFor(PortalSsoHarness.ReplayKeyPrefix + "payload-id");
        Assert.NotNull(options);
        Assert.Equal(TimeSpan.FromMinutes(11), options.AbsoluteExpirationRelativeToNow);
    }

    [Fact]
    public async Task An_exchange_that_denies_authentication_fails_the_state_without_issuing_a_session()
    {
        var harness = Pending(exchange: ExchangeResponses.Denied("Account is locked."));

        var result = await harness.Service.HandleCallbackAsync(
            harness.Callback(),
            IPAddress.Loopback,
            TestContext.Current.CancellationToken);

        Assert.Equal(SsoStateStatus.Failed, result.status);
        Assert.Equal("Account is locked.", result.message);
        Assert.Null(result.login);
        Assert.Equal("Account is locked.", harness.ReadState(State)!.ErrorMessage);
        await harness.SessionService.DidNotReceive()
            .IssueSessionAsync(Arg.Any<LoginResponse>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task An_exchange_denial_without_a_reason_uses_a_generic_message()
    {
        var harness = Pending(exchange: ExchangeResponses.Denied(null));

        var result = await harness.Service.HandleCallbackAsync(
            harness.Callback(),
            IPAddress.Loopback,
            TestContext.Current.CancellationToken);

        Assert.Equal("The SSO exchange did not authenticate the user.", result.message);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.Unauthorized)]
    public async Task An_unavailable_exchange_api_fails_the_state_and_surfaces_a_transport_error(
        HttpStatusCode statusCode)
    {
        var harness = Pending(exchange: ExchangeResponses.Failure(statusCode));

        await Assert.ThrowsAsync<HttpRequestException>(
            () => harness.Service.HandleCallbackAsync(
                harness.Callback(),
                IPAddress.Loopback,
                TestContext.Current.CancellationToken));

        Assert.Equal(SsoStateStatus.Failed, harness.ReadState(State)!.Status);
    }

    [Fact]
    public async Task An_empty_exchange_response_fails_the_state()
    {
        var harness = Pending(exchange: ExchangeResponses.Empty());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => harness.Service.HandleCallbackAsync(
                harness.Callback(),
                IPAddress.Loopback,
                TestContext.Current.CancellationToken));

        Assert.Contains("empty login response", exception.Message, StringComparison.Ordinal);
        Assert.Equal(SsoStateStatus.Failed, harness.ReadState(State)!.Status);
    }

    [Theory]
    [InlineData("BaseUrl")]
    [InlineData("Path")]
    public async Task An_unconfigured_exchange_api_fails_the_state(string missingSetting)
    {
        var harness = Pending(options =>
        {
            if (string.Equals(missingSetting, "BaseUrl", StringComparison.Ordinal))
                options.ExchangeApi.BaseUrl = string.Empty;
            else
                options.ExchangeApi.Path = string.Empty;
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => harness.Service.HandleCallbackAsync(
                harness.Callback(),
                IPAddress.Loopback,
                TestContext.Current.CancellationToken));

        Assert.Contains(missingSetting, exception.Message, StringComparison.Ordinal);
        Assert.Equal(SsoStateStatus.Failed, harness.ReadState(State)!.Status);
    }

    [Fact]
    public async Task A_failed_exchange_still_consumes_the_payload_id()
    {
        var harness = Pending(exchange: ExchangeResponses.Failure(HttpStatusCode.InternalServerError));

        await Assert.ThrowsAsync<HttpRequestException>(
            () => harness.Service.HandleCallbackAsync(
                harness.Callback(claims => claims["jti"] = "burned-payload-id"),
                IPAddress.Loopback,
                TestContext.Current.CancellationToken));

        Assert.True(harness.HasReplayMarker("burned-payload-id"));
    }

    [Fact]
    public async Task A_successful_callback_audits_the_user_without_the_session_token()
    {
        var harness = Pending();

        await harness.Service.HandleCallbackAsync(
            harness.Callback(),
            IPAddress.Loopback,
            TestContext.Current.CancellationToken);

        var entry = Assert.Single(harness.Logger.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Contains("devia", entry.AllText, StringComparison.Ordinal);
        Assert.DoesNotContain(PortalSsoHarness.IssuedSessionToken, entry.AllText, StringComparison.Ordinal);
        Assert.DoesNotContain("portal-session-token", entry.AllText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_failed_callback_is_audited_without_the_encrypted_payload_or_exchange_token()
    {
        var harness = Pending();
        var callback = harness.Callback(claims => claims["nonce"] = "wrong-nonce");

        await Assert.ThrowsAsync<SecurityTokenException>(
            () => harness.Service.HandleCallbackAsync(
                callback,
                IPAddress.Loopback,
                TestContext.Current.CancellationToken));

        var entry = Assert.Single(harness.Logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.DoesNotContain(callback.encryptedPayload, entry.AllText, StringComparison.Ordinal);
        Assert.DoesNotContain(SsoTokenFactory.ExchangeToken, entry.AllText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_failed_callback_writes_the_state_value_into_the_audit_record()
    {
        var harness = Pending();

        await Assert.ThrowsAsync<SecurityTokenException>(
            () => harness.Service.HandleCallbackAsync(
                harness.Callback(claims => claims["nonce"] = "wrong-nonce"),
                IPAddress.Loopback,
                TestContext.Current.CancellationToken));

        // The state doubles as the bearer value for FinalizeAsync, so logging it verbatim widens the
        // window in which a log reader could poll for the issued session.
        Assert.Contains(State, harness.Logger.AllLoggedText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_second_callback_validated_with_the_same_signing_key_fails_on_a_disposed_key()
    {
        var harness = Pending();

        var first = await harness.Service.HandleCallbackAsync(
            harness.Callback(),
            IPAddress.Loopback,
            TestContext.Current.CancellationToken);
        Assert.Equal(SsoStateStatus.Completed, first.status);

        harness.SeedPendingState("second-state", Nonce);

        var exception = await Assert.ThrowsAsync<SecurityTokenException>(
            () => harness.Service.HandleCallbackAsync(
                harness.Callback(state: "second-state"),
                IPAddress.Loopback,
                TestContext.Current.CancellationToken));

        // PRODUCT DEFECT: ValidatePayloadAsync wraps a `using var` RSA in an RsaSecurityKey, but
        // Microsoft.IdentityModel caches the resulting signature provider process-wide against the
        // key material. The next callback reuses that provider and hits the disposed RSA, so live
        // SSO logins fail intermittently after the first one. See IDX10517 / ObjectDisposedException.
        Assert.Contains("IDX10517", exception.Message, StringComparison.Ordinal);
    }

    private static PortalSsoHarness Pending(
        Action<PortalSsoOptions>? configure = null,
        Func<CapturedHttpRequest, HttpResponseMessage>? exchange = null)
    {
        var harness = new PortalSsoHarness(configure, exchange, SsoSigningKeyPair.Create());
        harness.SeedPendingState(State, Nonce);
        return harness;
    }
}
