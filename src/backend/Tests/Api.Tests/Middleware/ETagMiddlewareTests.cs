using System.Text;
using Api.Middleware;
using Microsoft.AspNetCore.Http;

namespace Api.Tests.Middleware;

public sealed class ETagMiddlewareTests
{
    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task Non_get_requests_are_streamed_straight_through(string method)
    {
        var result = await InvokeAsync(method, "payload");

        Assert.False(result.Headers.ContainsKey("ETag"));
        Assert.Equal("payload", result.Body);
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
    }

    [Fact]
    public async Task A_successful_get_response_is_tagged_and_still_delivered()
    {
        var result = await InvokeAsync(HttpMethods.Get, "purchase-order-42");

        var etag = result.Headers.ETag.ToString();
        Assert.StartsWith("\"", etag, StringComparison.Ordinal);
        Assert.EndsWith("\"", etag, StringComparison.Ordinal);
        Assert.Equal(24, etag.Length);
        Assert.Equal("purchase-order-42", result.Body);
    }

    [Fact]
    public async Task The_same_payload_always_produces_the_same_tag()
    {
        var first = await InvokeAsync(HttpMethods.Get, "stable");
        var second = await InvokeAsync(HttpMethods.Get, "stable");

        Assert.Equal(first.Headers.ETag.ToString(), second.Headers.ETag.ToString());
    }

    [Fact]
    public async Task A_changed_payload_produces_a_different_tag()
    {
        var first = await InvokeAsync(HttpMethods.Get, "version-1");
        var second = await InvokeAsync(HttpMethods.Get, "version-2");

        Assert.NotEqual(first.Headers.ETag.ToString(), second.Headers.ETag.ToString());
    }

    [Fact]
    public async Task A_matching_if_none_match_short_circuits_with_not_modified_and_no_body()
    {
        var first = await InvokeAsync(HttpMethods.Get, "cacheable");

        var second = await InvokeAsync(
            HttpMethods.Get,
            "cacheable",
            ifNoneMatch: first.Headers.ETag.ToString());

        Assert.Equal(StatusCodes.Status304NotModified, second.StatusCode);
        Assert.Equal(0, second.ContentLength);
        Assert.Equal(string.Empty, second.Body);
    }

    [Fact]
    public async Task A_stale_if_none_match_returns_the_current_payload()
    {
        var result = await InvokeAsync(HttpMethods.Get, "cacheable", ifNoneMatch: "\"an-old-tag\"");

        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        Assert.Equal("cacheable", result.Body);
    }

    [Fact]
    public async Task An_empty_successful_response_is_not_tagged()
    {
        var result = await InvokeAsync(HttpMethods.Get, string.Empty);

        Assert.False(result.Headers.ContainsKey("ETag"));
    }

    [Theory]
    [InlineData(StatusCodes.Status201Created)]
    [InlineData(StatusCodes.Status404NotFound)]
    [InlineData(StatusCodes.Status500InternalServerError)]
    public async Task Only_200_responses_are_tagged(int statusCode)
    {
        var result = await InvokeAsync(HttpMethods.Get, "body", statusCode: statusCode);

        Assert.False(result.Headers.ContainsKey("ETag"));
        Assert.Equal("body", result.Body);
    }

    private static async Task<Recorded> InvokeAsync(
        string method,
        string payload,
        string? ifNoneMatch = null,
        int statusCode = StatusCodes.Status200OK)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        if (ifNoneMatch is not null)
        {
            context.Request.Headers.IfNoneMatch = ifNoneMatch;
        }

        var transport = new MemoryStream();
        context.Response.Body = transport;

        var middleware = new ETagMiddleware(async downstream =>
        {
            downstream.Response.StatusCode = statusCode;
            if (payload.Length > 0)
            {
                await downstream.Response.WriteAsync(payload, TestContext.Current.CancellationToken);
            }
        });

        await middleware.InvokeAsync(context);

        return new Recorded(
            context.Response.StatusCode,
            context.Response.Headers,
            context.Response.ContentLength,
            Encoding.UTF8.GetString(transport.ToArray()));
    }

    private sealed record Recorded(
        int StatusCode,
        IHeaderDictionary Headers,
        long? ContentLength,
        string Body);
}
