namespace Auth.Models;

/// <summary>
/// Optional "sign in with ..." slot, bound from the <c>ExternalIdp</c> configuration section.
/// <para>
/// The template ships with this feature switched OFF and with no credentials. The local identity
/// provider (<c>UserAccounts</c> table) is the default and is entirely self-contained.
/// </para>
/// <para>
/// To enable it for your own project:
/// <list type="number">
///   <item><description>Register an OAuth/OIDC application with the provider and note the client id + secret.</description></item>
///   <item><description>Set the redirect (callback) URL there to your Auth API's
///     <c>/api/Auth/ExternalCallback</c> URL, and put the same value in <see cref="ExternalProviderOptions.RedirectUri"/>.</description></item>
///   <item><description>Set <see cref="Enabled"/> to true and the provider's own
///     <see cref="ExternalProviderOptions.Enabled"/> to true.</description></item>
///   <item><description>Keep the client secret out of source control - use user-secrets or
///     environment variables (<c>ExternalIdp__Providers__Google__ClientSecret</c>).</description></item>
/// </list>
/// </para>
/// <para>
/// Well-known authorities: Google <c>https://accounts.google.com</c>,
/// Microsoft Entra ID <c>https://login.microsoftonline.com/{tenant}/v2.0</c>.
/// GitHub is plain OAuth 2.0 with no discovery document, so set its three endpoint
/// overrides explicitly (see <see cref="ExternalProviderOptions.AuthorizationEndpoint"/>).
/// </para>
/// </summary>
public class ExternalIdpOptions
{
    /// <summary>
    /// The configuration section this class binds to.
    /// </summary>
    public const string SectionName = "ExternalIdp";

    /// <summary>
    /// Master switch. When false the external endpoints answer 503 and
    /// <c>GET /api/Auth/ExternalProviders</c> returns an empty array.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// The configured providers, keyed by a short provider name ("Google", "Microsoft", "GitHub").
    /// The key is what callers pass as <c>?provider=</c> and is compared case-insensitively.
    /// </summary>
    public Dictionary<string, ExternalProviderOptions> Providers { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Returns the providers that are usable right now: the master switch is on, the provider
    /// itself is enabled, and it has enough configuration to complete a sign-in.
    /// </summary>
    public IReadOnlyList<KeyValuePair<string, ExternalProviderOptions>> GetUsableProviders()
    {
        if (!Enabled)
            return [];

        return Providers
            .Where(entry => entry.Value.Enabled && entry.Value.IsConfigured())
            .ToList();
    }

    /// <summary>
    /// Looks up a usable provider by name. Returns null when the slot is off, the name is unknown,
    /// the provider is disabled, or the provider is missing configuration.
    /// </summary>
    public ExternalProviderOptions? FindUsableProvider(string? providerName)
    {
        if (!Enabled || string.IsNullOrWhiteSpace(providerName))
            return null;

        if (!Providers.TryGetValue(providerName.Trim(), out var provider))
            return null;

        return provider.Enabled && provider.IsConfigured() ? provider : null;
    }
}

/// <summary>
/// Settings for a single external identity provider.
/// </summary>
public class ExternalProviderOptions
{
    /// <summary>
    /// Whether this provider is offered on the sign-in page. Defaults to false.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Label shown on the sign-in button. Falls back to the dictionary key when empty.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// The OAuth client (application) id issued by the provider.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// The OAuth client secret issued by the provider. Store this outside source control.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// The OIDC issuer base URL. Its <c>/.well-known/openid-configuration</c> document is read
    /// to discover the authorization, token and userinfo endpoints.
    /// Leave empty and set the endpoint overrides instead for non-OIDC providers such as GitHub.
    /// </summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>
    /// Space-separated scopes requested at the authorization endpoint,
    /// for example <c>openid profile email</c>.
    /// </summary>
    public string Scopes { get; set; } = "openid profile email";

    /// <summary>
    /// The absolute callback URL registered with the provider. It must point at this API's
    /// <c>/api/Auth/ExternalCallback</c> action. Leave empty to derive it from the incoming request.
    /// </summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>
    /// Optional override for the authorization endpoint. Required when <see cref="Authority"/>
    /// has no discovery document (GitHub: <c>https://github.com/login/oauth/authorize</c>).
    /// </summary>
    public string AuthorizationEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Optional override for the token endpoint
    /// (GitHub: <c>https://github.com/login/oauth/access_token</c>).
    /// </summary>
    public string TokenEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Optional override for the userinfo endpoint, used to read the profile when the provider
    /// returns no id_token (GitHub: <c>https://api.github.com/user</c>).
    /// </summary>
    public string UserInfoEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Whether an unknown external subject may create a local <c>UserAccount</c> on first sign-in.
    /// When false, only users who already have an account can sign in through this provider.
    /// </summary>
    public bool AllowAutoProvision { get; set; } = true;

    /// <summary>
    /// Whether the provider supplies enough configuration to attempt a sign-in.
    /// </summary>
    public bool IsConfigured() =>
        !string.IsNullOrWhiteSpace(ClientId)
        && !string.IsNullOrWhiteSpace(ClientSecret)
        && (!string.IsNullOrWhiteSpace(Authority)
            || (!string.IsNullOrWhiteSpace(AuthorizationEndpoint) && !string.IsNullOrWhiteSpace(TokenEndpoint)));
}
