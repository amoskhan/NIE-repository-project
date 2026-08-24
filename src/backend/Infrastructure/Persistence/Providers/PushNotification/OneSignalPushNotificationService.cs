using System.Net.Http.Json;
using System.Text.Json;
using Application.Abstractions.Identity;
using Application.Features.Email;
using Application.Features.PushNotification;
using Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Providers.PushNotification;

public class OneSignalPushNotificationService : IPushNotificationService
{
    private readonly OneSignalSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<OneSignalPushNotificationService> _logger;
    private const string OneSignalApiUrl = "https://api.onesignal.com/notifications";

    public OneSignalPushNotificationService(
        IOptions<OneSignalSettings> settings,
        HttpClient httpClient,
        ILogger<OneSignalPushNotificationService> logger)
    {
        _settings = settings.Value;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task SendToUsersAsync(IEnumerable<string> externalUserIds, string title, string message, string? url = null, IDictionary<string, string>? data = null)
    {
        if (!IsConfigured()) return;

        var userIds = externalUserIds.ToList();
        if (userIds.Count == 0) return;

        var payload = BuildPayload(title, message, url, data);
        payload["include_aliases"] = new { external_id = userIds };
        payload["target_channel"] = "push";

        await SendNotificationAsync(payload);
    }

    public async Task SendToAllAsync(string title, string message, string? url = null, IDictionary<string, string>? data = null)
    {
        if (!IsConfigured()) return;

        var payload = BuildPayload(title, message, url, data);
        payload["included_segments"] = new[] { "Subscribed Users" };

        await SendNotificationAsync(payload);
    }

    public async Task SendToSegmentAsync(string segment, string title, string message, string? url = null, IDictionary<string, string>? data = null)
    {
        if (!IsConfigured()) return;

        var payload = BuildPayload(title, message, url, data);
        payload["included_segments"] = new[] { segment };

        await SendNotificationAsync(payload);
    }

    private bool IsConfigured()
    {
        if (string.IsNullOrWhiteSpace(_settings.AppId) || string.IsNullOrWhiteSpace(_settings.RestApiKey))
        {
            _logger.LogDebug("OneSignal is not configured (AppId or RestApiKey missing). Skipping push notification.");
            return false;
        }
        return true;
    }

    private Dictionary<string, object> BuildPayload(string title, string message, string? url, IDictionary<string, string>? data)
    {
        var payload = new Dictionary<string, object>
        {
            ["app_id"] = _settings.AppId,
            ["headings"] = new { en = title },
            ["contents"] = new { en = message }
        };

        if (!string.IsNullOrWhiteSpace(url))
            payload["url"] = url;

        if (data is { Count: > 0 })
            payload["data"] = data;

        return payload;
    }

    private async Task SendNotificationAsync(Dictionary<string, object> payload)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, OneSignalApiUrl);
            request.Headers.Add("Authorization", $"Key {_settings.RestApiKey}");
            request.Content = JsonContent.Create(payload);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("OneSignal API returned {StatusCode}: {Body}", response.StatusCode, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send OneSignal push notification");
        }
    }
}
