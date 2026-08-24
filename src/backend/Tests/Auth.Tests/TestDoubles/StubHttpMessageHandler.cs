using System.Net;
using System.Text;
using NSubstitute;

namespace Auth.Tests.TestDoubles;

internal sealed record CapturedHttpRequest(
    HttpMethod Method,
    string Url,
    IReadOnlyDictionary<string, string> Headers,
    string Body);

/// <summary>
/// Captures the outbound request and replays a canned response. Used instead of NSubstitute because
/// <see cref="HttpMessageHandler.SendAsync"/> is protected and cannot be configured on a substitute.
/// </summary>
internal sealed class StubHttpMessageHandler(Func<CapturedHttpRequest, HttpResponseMessage> responder)
    : HttpMessageHandler
{
    private readonly List<CapturedHttpRequest> _requests = [];

    public IReadOnlyList<CapturedHttpRequest> Requests => _requests;

    public static StubHttpMessageHandler Json(HttpStatusCode statusCode, string json) =>
        new(_ => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

    public static StubHttpMessageHandler Ok(string json) => Json(HttpStatusCode.OK, json);

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var body = request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken);

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in request.Headers)
            headers[header.Key] = string.Join(',', header.Value);

        var captured = new CapturedHttpRequest(
            request.Method,
            request.RequestUri?.ToString() ?? string.Empty,
            headers,
            body);

        _requests.Add(captured);
        return responder(captured);
    }
}

internal static class HttpClientFactoryStub
{
    public static IHttpClientFactory Create(HttpMessageHandler handler)
    {
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient(handler, disposeHandler: false));
        return factory;
    }
}
