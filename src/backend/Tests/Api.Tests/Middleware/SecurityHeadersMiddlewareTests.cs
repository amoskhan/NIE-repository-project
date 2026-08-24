using Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Api.Tests.Middleware;

public sealed class SecurityHeadersMiddlewareTests
{
    [Fact]
    public async Task Every_response_carries_the_owasp_baseline_headers()
    {
        var options = new SecurityHeadersOptions();

        var headers = await InvokeAsync(options, "/api/vendor/getall");

        Assert.Equal(options.ContentSecurityPolicy, headers["Content-Security-Policy"].ToString());
        Assert.Equal(options.StrictTransportSecurity, headers["Strict-Transport-Security"].ToString());
        Assert.Equal("DENY", headers["X-Frame-Options"].ToString());
        Assert.Equal("nosniff", headers["X-Content-Type-Options"].ToString());
        Assert.Equal("strict-origin-when-cross-origin", headers["Referrer-Policy"].ToString());
        Assert.Equal(options.PermissionsPolicy, headers["Permissions-Policy"].ToString());
    }

    [Fact]
    public async Task The_default_policy_denies_framing_and_locks_the_base_uri()
    {
        var headers = await InvokeAsync(new SecurityHeadersOptions(), "/");

        var policy = headers["Content-Security-Policy"].ToString();
        Assert.Contains("frame-ancestors 'none'", policy, StringComparison.Ordinal);
        Assert.Contains("base-uri 'self'", policy, StringComparison.Ordinal);
        Assert.Contains("default-src 'self'", policy, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/openapi")]
    [InlineData("/openapi/v1.json")]
    public async Task The_openapi_document_is_exempt_from_the_content_security_policy(string path)
    {
        var headers = await InvokeAsync(new SecurityHeadersOptions(), path);

        Assert.False(headers.ContainsKey("Content-Security-Policy"));
        Assert.Equal("DENY", headers["X-Frame-Options"].ToString());
    }

    [Fact]
    public async Task The_openapi_exemption_can_be_switched_off()
    {
        var options = new SecurityHeadersOptions { EnableForOpenApi = true };

        var headers = await InvokeAsync(options, "/openapi/v1.json");

        Assert.Equal(options.ContentSecurityPolicy, headers["Content-Security-Policy"].ToString());
    }

    [Theory]
    [InlineData("/openapidocs")]
    [InlineData("/openapi-viewer/index.html")]
    public async Task Only_the_openapi_path_segment_is_exempt(string path)
    {
        var headers = await InvokeAsync(new SecurityHeadersOptions(), path);

        Assert.True(headers.ContainsKey("Content-Security-Policy"));
    }

    [Fact]
    public async Task Configured_values_replace_the_defaults()
    {
        var options = new SecurityHeadersOptions
        {
            ContentSecurityPolicy = "default-src 'none'",
            StrictTransportSecurity = "max-age=60",
            XFrameOptions = "SAMEORIGIN",
            XContentTypeOptions = "nosniff",
            ReferrerPolicy = "no-referrer",
            PermissionsPolicy = "camera=()",
        };

        var headers = await InvokeAsync(options, "/api/vendor/getall");

        Assert.Equal("default-src 'none'", headers["Content-Security-Policy"].ToString());
        Assert.Equal("max-age=60", headers["Strict-Transport-Security"].ToString());
        Assert.Equal("SAMEORIGIN", headers["X-Frame-Options"].ToString());
        Assert.Equal("no-referrer", headers["Referrer-Policy"].ToString());
        Assert.Equal("camera=()", headers["Permissions-Policy"].ToString());
    }

    [Fact]
    public async Task Headers_are_written_before_the_pipeline_runs_so_a_failure_cannot_strip_them()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/vendor/getall";
        var middleware = new SecurityHeadersMiddleware(
            _ => throw new InvalidOperationException("downstream exploded"),
            Options.Create(new SecurityHeadersOptions()));

        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));

        Assert.True(context.Response.Headers.ContainsKey("Content-Security-Policy"));
        Assert.True(context.Response.Headers.ContainsKey("Strict-Transport-Security"));
    }

    [Fact]
    public async Task Headers_are_visible_to_the_rest_of_the_pipeline()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/vendor/getall";
        string? observedFrameOptions = null;
        var middleware = new SecurityHeadersMiddleware(
            downstream =>
            {
                observedFrameOptions = downstream.Response.Headers["X-Frame-Options"].ToString();
                return Task.CompletedTask;
            },
            Options.Create(new SecurityHeadersOptions()));

        await middleware.InvokeAsync(context);

        Assert.Equal("DENY", observedFrameOptions);
    }

    private static async Task<IHeaderDictionary> InvokeAsync(SecurityHeadersOptions options, string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask, Options.Create(options));

        await middleware.InvokeAsync(context);

        return context.Response.Headers;
    }
}
