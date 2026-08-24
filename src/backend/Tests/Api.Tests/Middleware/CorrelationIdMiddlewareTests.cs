using Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;

namespace Api.Tests.Middleware;

public sealed class CorrelationIdMiddlewareTests
{
    private const string HeaderName = "X-Correlation-Id";

    [Fact]
    public async Task An_inbound_correlation_id_is_propagated_unchanged()
    {
        var context = CreateHttpContext(out var responseFeature);
        context.Request.Headers[HeaderName] = "upstream-42";

        await InvokeAsync(context);
        await responseFeature.StartResponseAsync();

        Assert.Equal("upstream-42", context.Items["CorrelationId"]);
        Assert.Equal("upstream-42", context.Response.Headers[HeaderName].ToString());
    }

    [Fact]
    public async Task The_first_inbound_header_value_wins()
    {
        var context = CreateHttpContext(out var responseFeature);
        context.Request.Headers[HeaderName] = new[] { "first", "second" };

        await InvokeAsync(context);
        await responseFeature.StartResponseAsync();

        Assert.Equal("first", context.Response.Headers[HeaderName].ToString());
    }

    [Fact]
    public async Task A_request_without_a_correlation_id_gets_a_generated_hex_identifier()
    {
        var context = CreateHttpContext(out var responseFeature);

        await InvokeAsync(context);
        await responseFeature.StartResponseAsync();

        var correlationId = Assert.IsType<string>(context.Items["CorrelationId"]);
        Assert.Equal(32, correlationId.Length);
        Assert.All(correlationId, character => Assert.True(Uri.IsHexDigit(character)));
        Assert.Equal(correlationId, context.Response.Headers[HeaderName].ToString());
    }

    [Fact]
    public async Task Generated_correlation_ids_are_unique_per_request()
    {
        var first = CreateHttpContext(out _);
        var second = CreateHttpContext(out _);

        await InvokeAsync(first);
        await InvokeAsync(second);

        Assert.NotEqual(first.Items["CorrelationId"], second.Items["CorrelationId"]);
    }

    [Fact]
    public async Task Downstream_middleware_can_read_the_correlation_id()
    {
        var context = CreateHttpContext(out _);
        context.Request.Headers[HeaderName] = "upstream-42";
        object? observed = null;

        await InvokeAsync(context, downstream =>
        {
            observed = downstream.Items["CorrelationId"];
            return Task.CompletedTask;
        });

        Assert.Equal("upstream-42", observed);
    }

    [Fact]
    public async Task The_response_header_is_only_written_once_the_response_starts()
    {
        var context = CreateHttpContext(out var responseFeature);

        await InvokeAsync(context);

        Assert.False(context.Response.Headers.ContainsKey(HeaderName));
        await responseFeature.StartResponseAsync();
        Assert.True(context.Response.Headers.ContainsKey(HeaderName));
    }

    private static Task InvokeAsync(HttpContext context, RequestDelegate? next = null) =>
        new CorrelationIdMiddleware(next ?? (_ => Task.CompletedTask))
            .InvokeAsync(context, NullLogger<CorrelationIdMiddleware>.Instance);

    private static DefaultHttpContext CreateHttpContext(out DeferredResponseFeature responseFeature)
    {
        responseFeature = new DeferredResponseFeature();
        var context = new DefaultHttpContext();
        context.Features.Set<IHttpResponseFeature>(responseFeature);
        return context;
    }

    /// <summary>
    /// <see cref="HttpResponseFeature"/> ignores <c>OnStarting</c> callbacks, so this
    /// records them and lets a test decide when the response "starts".
    /// </summary>
    private sealed class DeferredResponseFeature : HttpResponseFeature
    {
        private readonly List<(Func<object, Task> Callback, object State)> _callbacks = [];

        public override void OnStarting(Func<object, Task> callback, object state) =>
            _callbacks.Add((callback, state));

        public async Task StartResponseAsync()
        {
            foreach (var (callback, state) in _callbacks)
            {
                await callback(state);
            }
        }
    }
}
