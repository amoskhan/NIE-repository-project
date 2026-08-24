using Microsoft.Extensions.Options;

namespace Api.Middleware;

/// <summary>
/// Adds OWASP-recommended security response headers (CSP, HSTS, X-Frame-Options,
/// X-Content-Type-Options, Referrer-Policy, Permissions-Policy) to every response.
/// Driven by <see cref="SecurityHeadersOptions"/>.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityHeadersOptions _options;

    public SecurityHeadersMiddleware(RequestDelegate next, IOptions<SecurityHeadersOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Set headers BEFORE calling _next so they apply even if a downstream middleware
        // short-circuits (e.g. authorization redirects, exception handler).
        var headers = context.Response.Headers;
        var isOpenApiDocument = context.Request.Path.StartsWithSegments("/openapi");

        if (!isOpenApiDocument || _options.EnableForOpenApi)
        {
            headers["Content-Security-Policy"] = _options.ContentSecurityPolicy;
        }

        headers["Strict-Transport-Security"] = _options.StrictTransportSecurity;
        headers["X-Frame-Options"] = _options.XFrameOptions;
        headers["X-Content-Type-Options"] = _options.XContentTypeOptions;
        headers["Referrer-Policy"] = _options.ReferrerPolicy;
        headers["Permissions-Policy"] = _options.PermissionsPolicy;

        await _next(context);
    }
}

public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.UseMiddleware<SecurityHeadersMiddleware>();
}
