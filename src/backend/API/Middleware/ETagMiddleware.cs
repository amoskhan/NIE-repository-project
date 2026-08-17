using System.Security.Cryptography;
using System.Text;

namespace API.Middleware;

/// <summary>
/// Middleware that generates ETag headers for GET responses and handles
/// If-None-Match conditional requests to return 304 Not Modified.
/// </summary>
public class ETagMiddleware
{
    private readonly RequestDelegate _next;

    public ETagMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only process GET requests
        if (!HttpMethods.IsGet(context.Request.Method))
        {
            await _next(context);
            return;
        }

        var originalStream = context.Response.Body;
        using var memoryStream = new MemoryStream();
        context.Response.Body = memoryStream;

        await _next(context);

        // Only add ETag for successful responses with content
        if (context.Response.StatusCode == StatusCodes.Status200OK && memoryStream.Length > 0)
        {
            memoryStream.Position = 0;
            var hash = SHA256.HashData(memoryStream.ToArray());
            var etag = $"\"{Convert.ToBase64String(hash)[..22]}\"";

            context.Response.Headers.ETag = etag;

            if (context.Request.Headers.IfNoneMatch.ToString() == etag)
            {
                context.Response.StatusCode = StatusCodes.Status304NotModified;
                context.Response.ContentLength = 0;
                context.Response.Body = originalStream;
                return;
            }
        }

        memoryStream.Position = 0;
        await memoryStream.CopyToAsync(originalStream);
        context.Response.Body = originalStream;
    }
}
