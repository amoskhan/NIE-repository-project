using Auth.Models;

namespace Auth.Services;

/// <summary>
/// The optional "sign in with Google/Microsoft/GitHub" slot.
/// <para>
/// Everything here is inert until the <c>ExternalIdp</c> configuration section is switched on -
/// <see cref="IsEnabled"/> is false and the controller answers 503. When enabled it runs a
/// standard OAuth 2.0 / OpenID Connect authorization-code flow with PKCE, then hands the resulting
/// identity to <see cref="ILocalIdentityService.ResolveExternalUserAsync"/> and mints exactly the
/// same Valkey session a local password sign-in would produce.
/// </para>
/// </summary>
public interface IExternalIdpService
{
    /// <summary>
    /// Whether at least one provider is enabled AND fully configured.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// The providers a sign-in page should offer. Empty when the slot is off.
    /// </summary>
    IReadOnlyList<ExternalProviderSummary> GetEnabledProviders();

    /// <summary>
    /// Begins a sign-in: generates state + PKCE, parks them in Valkey, and returns the provider's
    /// authorization URL that the browser should be redirected to.
    /// </summary>
    /// <param name="providerName">Key from the <c>ExternalIdp:Providers</c> dictionary.</param>
    /// <param name="returnUrl">Where to send the browser after a successful sign-in.</param>
    /// <param name="fallbackRedirectUri">
    /// Callback URL derived from the current request, used when the provider has no configured
    /// <see cref="ExternalProviderOptions.RedirectUri"/>.
    /// </param>
    Task<string> BuildAuthorizationUrlAsync(
        string providerName,
        string? returnUrl,
        string fallbackRedirectUri,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes a sign-in: redeems the one-time state, exchanges the authorization code for
    /// tokens, reads the user's profile and issues an application session.
    /// </summary>
    Task<ExternalCallbackResult> HandleCallbackAsync(
        string? code,
        string? state,
        CancellationToken cancellationToken = default);
}
