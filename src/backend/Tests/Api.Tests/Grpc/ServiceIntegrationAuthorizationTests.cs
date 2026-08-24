using System.Security.Claims;
using Api.Grpc;

namespace Api.Tests.Grpc;

/// <summary>
/// Scope evaluation for inbound service-to-service gRPC calls. A false positive here
/// hands another service more authority than it was issued.
/// </summary>
public sealed class ServiceIntegrationAuthorizationTests
{
    private const string RequiredScope = "nie.procurement.read";

    [Fact]
    public void A_missing_principal_is_a_programming_error()
    {
        Assert.Throws<ArgumentNullException>(
            () => ServiceIntegrationAuthorization.HasRequiredScope(null!, RequiredScope));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void An_unspecified_required_scope_never_grants_access(string? requiredScope)
    {
        var principal = CreatePrincipal(("scope", RequiredScope));

        Assert.False(ServiceIntegrationAuthorization.HasRequiredScope(principal, requiredScope!));
    }

    [Fact]
    public void A_principal_without_any_claims_is_denied()
    {
        Assert.False(ServiceIntegrationAuthorization.HasRequiredScope(new ClaimsPrincipal(), RequiredScope));
    }

    [Theory]
    [InlineData("scope")]
    [InlineData("scp")]
    public void Both_the_oauth_and_the_microsoft_scope_claim_types_are_honoured(string claimType)
    {
        var principal = CreatePrincipal((claimType, $"nie.other.read {RequiredScope} nie.other.write"));

        Assert.True(ServiceIntegrationAuthorization.HasRequiredScope(principal, RequiredScope));
    }

    [Fact]
    public void Scopes_spread_across_several_claims_are_combined()
    {
        var principal = CreatePrincipal(
            ("scope", "nie.other.read"),
            ("scp", RequiredScope));

        Assert.True(ServiceIntegrationAuthorization.HasRequiredScope(principal, RequiredScope));
    }

    [Fact]
    public void Padding_and_empty_entries_in_the_scope_claim_are_ignored()
    {
        var principal = CreatePrincipal(("scope", $"   nie.other.read    {RequiredScope}   "));

        Assert.True(ServiceIntegrationAuthorization.HasRequiredScope(principal, RequiredScope));
    }

    [Theory]
    [InlineData("NIE.Procurement.Read")]
    [InlineData("nie.procurement.READ")]
    public void Scope_matching_is_case_sensitive(string grantedScope)
    {
        var principal = CreatePrincipal(("scope", grantedScope));

        Assert.False(ServiceIntegrationAuthorization.HasRequiredScope(principal, RequiredScope));
    }

    [Theory]
    [InlineData("nie.procurement.readonly")]
    [InlineData("nie.procurement")]
    [InlineData("xnie.procurement.read")]
    public void A_partial_scope_match_does_not_grant_access(string grantedScope)
    {
        var principal = CreatePrincipal(("scope", grantedScope));

        Assert.False(ServiceIntegrationAuthorization.HasRequiredScope(principal, RequiredScope));
    }

    [Theory]
    [InlineData("scopes")]
    [InlineData("role")]
    [InlineData("http://schemas.microsoft.com/identity/claims/scope")]
    public void Look_alike_claim_types_do_not_grant_access(string claimType)
    {
        var principal = CreatePrincipal((claimType, RequiredScope));

        Assert.False(ServiceIntegrationAuthorization.HasRequiredScope(principal, RequiredScope));
    }

    [Fact]
    public void A_comma_separated_scope_claim_is_not_split()
    {
        var principal = CreatePrincipal(("scope", $"nie.other.read,{RequiredScope}"));

        Assert.False(ServiceIntegrationAuthorization.HasRequiredScope(principal, RequiredScope));
    }

    private static ClaimsPrincipal CreatePrincipal(params (string Type, string Value)[] claims) =>
        new(new ClaimsIdentity(
            claims.Select(claim => new Claim(claim.Type, claim.Value)),
            authenticationType: "ServiceIntegration"));
}
