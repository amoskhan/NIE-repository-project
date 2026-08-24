using System.Text.Json;
using Application.Contracts;
using BuildingBlocks.Helpers;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Providers.StaffDirectory;

public sealed class StaffDirectoryService : IStaffDirectoryService
{
    private const string GatewayKeyHeader = "x-nie-aws-api-gw-key";
    private static readonly TimeSpan TokenCacheLifetime = TimeSpan.FromMinutes(55);
    private static readonly TimeSpan LookupCacheLifetime = TimeSpan.FromMinutes(5);
    private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _httpClient;
    private readonly IDistributedCache? _cache;
    private readonly ILogger<StaffDirectoryService> _logger;
    private readonly string? _baseUrl;
    private readonly string? _tokenEndpoint;
    private readonly string? _staffDetailsEndpoint;
    private readonly string? _subscriptionKey;
    private readonly string? _appId;
    private readonly string[] _allowedHosts;

    public StaffDirectoryService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<StaffDirectoryService> logger,
        IDistributedCache? cache = null)
    {
        _httpClient = httpClient;
        _logger = logger;
        _cache = cache;
        _baseUrl = configuration["ExternalAPIs:BaseUrl"];
        _tokenEndpoint = configuration["ExternalAPIs:TokenEndpoint"];
        _staffDetailsEndpoint = configuration["ExternalAPIs:StaffService:StaffDetailsEndpoint"];
        _subscriptionKey = configuration["ExternalAPIs:SubscriptionKey"];
        _appId = configuration["ExternalAPIs:StaffService:AppId"];
        _allowedHosts = configuration.GetSection("ExternalAPIs:AllowedHosts").Get<string[]>() ?? [];
    }

    public async Task<StaffDetailsDto?> GetStaffDetailsByEmailAsync(string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var cacheKey = $"staff_directory_lookup_{normalizedEmail}";
        var cached = await ReadCacheAsync(cacheKey);
        if (cached is not null)
        {
            return Deserialize<StaffDetailsDto>(cached);
        }

        EnsureConfigured();
        var token = await GetTokenAsync();
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            BuildEndpointUri(_staffDetailsEndpoint!, "NIE staff details endpoint"));
        request.Headers.Add(GatewayKeyHeader, _subscriptionKey);
        request.Headers.Add("AppId", _appId);
        request.Headers.Add("Email", normalizedEmail);
        request.Headers.Add("Authorization", $"Bearer {token}");

        using var response = await SendAsync(request, "staff details");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        if (!response.IsSuccessStatusCode)
        {
            throw new StaffDirectoryUnavailableException("Staff directory lookup failed.");
        }

        var records = Deserialize<StaffDetailsResponseDto[]>(await response.Content.ReadAsStringAsync());
        var record = records?.FirstOrDefault();
        if (record is null)
        {
            return null;
        }

        var staff = new StaffDetailsDto
        {
            WorkerId = record.WorkerId,
            UserId = record.UserId.Trim().ToLowerInvariant(),
            Name = record.Name,
            Department = record.Department,
            DepartmentDescription = record.DepartmentDescription,
            Email = record.Email,
            Designation = record.Designation,
            JoiningDate = record.JoiningDate,
            Title = record.Title
        };
        await WriteCacheAsync(cacheKey, JsonSerializer.Serialize(staff), LookupCacheLifetime);
        return staff;
    }

    private async Task<string> GetTokenAsync()
    {
        const string cacheKey = "staff_directory_token";
        var cached = await ReadCacheAsync(cacheKey);
        if (!string.IsNullOrWhiteSpace(cached))
        {
            return cached;
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            BuildEndpointUri(_tokenEndpoint!, "NIE staff directory token endpoint"));
        request.Headers.Add(GatewayKeyHeader, _subscriptionKey);
        using var response = await SendAsync(request, "token");
        if (!response.IsSuccessStatusCode)
        {
            throw new StaffDirectoryUnavailableException("Staff directory token request failed.");
        }

        var token = (await response.Content.ReadAsStringAsync()).Trim();
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new StaffDirectoryUnavailableException("Staff directory token response was empty.");
        }
        await WriteCacheAsync(cacheKey, token, TokenCacheLifetime);
        return token;
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_baseUrl) ||
            string.IsNullOrWhiteSpace(_tokenEndpoint) ||
            string.IsNullOrWhiteSpace(_staffDetailsEndpoint) ||
            string.IsNullOrWhiteSpace(_subscriptionKey) ||
            string.IsNullOrWhiteSpace(_appId) ||
            _allowedHosts.Length == 0)
        {
            throw new StaffDirectoryUnavailableException("Staff directory is not configured.");
        }
    }

    private Uri BuildEndpointUri(string endpoint, string context) =>
        SsrfGuard.Validate($"{_baseUrl!.TrimEnd('/')}/{endpoint.TrimStart('/')}", _allowedHosts, context);

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, string operation)
    {
        try
        {
            return await _httpClient.SendAsync(request);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(exception, "NIE staff directory {Operation} request failed.", operation);
            throw new StaffDirectoryUnavailableException("Staff directory is unavailable.", exception);
        }
    }

    private async Task<string?> ReadCacheAsync(string key)
    {
        try { return _cache is null ? null : await _cache.GetStringAsync(key); }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Could not read staff directory cache entry {Key}.", key);
            return null;
        }
    }

    private async Task WriteCacheAsync(string key, string value, TimeSpan lifetime)
    {
        if (_cache is null) return;
        try
        {
            await _cache.SetStringAsync(key, value, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = lifetime
            });
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Could not write staff directory cache entry {Key}.", key);
        }
    }

    private T? Deserialize<T>(string payload)
    {
        try { return JsonSerializer.Deserialize<T>(payload, SerializerOptions); }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "NIE staff directory returned an invalid payload.");
            throw new StaffDirectoryUnavailableException("Staff directory response was invalid.", exception);
        }
    }
}
