using System.Text.Json;
using Api.Authorization;
using Application.Contracts;
using Application.Features.MyInfo;
using Application.Security;
using BuildingBlocks.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

namespace Api.Controllers;

public class MyInfoController : BaseController
{
    private const string MyInfoStateCachePrefix = "myinfo:state:";
    private static readonly TimeSpan MyInfoStateLifetime = TimeSpan.FromMinutes(10);

    private readonly IMyInfoService _myInfoService;
    private readonly IDistributedCache _distributedCache;
    private readonly string _testProfilesPath;

    public MyInfoController(
        IMyInfoService myInfoService,
        IDistributedCache distributedCache)
    {
        _myInfoService = myInfoService;
        _distributedCache = distributedCache;
        _testProfilesPath = Path.Combine(AppContext.BaseDirectory, "Resources", "test-profiles.json");
    }

    [HttpGet]
    [RequireAccessFunction(AccessFunctionCodes.Api.MyInfoUse)]
    public async Task<IActionResult> GetAuthorizeUrl(CancellationToken cancellationToken)
    {
        if (!_myInfoService.IsConfigured)
            return BadRequest(new { message = "MyInfo/Singpass is not configured" });

        var stateId = Guid.NewGuid().ToString("N");
        var issuedAtUtc = DateTimeHelper.UtcOffsetNow;
        var authorizationRequest = await _myInfoService.CreateAuthorizationRequestAsync(stateId, cancellationToken);

        await _distributedCache.SetStringAsync(
            GetStateCacheKey(stateId),
            JsonSerializer.Serialize(new MyInfoAuthSessionState(
                stateId,
                authorizationRequest.CodeVerifier,
                authorizationRequest.Nonce,
                authorizationRequest.DpopPrivateKey,
                issuedAtUtc)),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = MyInfoStateLifetime,
            },
            cancellationToken);

        return Ok(new { authorizeUrl = authorizationRequest.AuthorizeUrl });
    }

    [HttpPost]
    [RequireAccessFunction(AccessFunctionCodes.Api.MyInfoUse)]
    public async Task<IActionResult> Callback(
        [FromBody] MyInfoCallbackRequest request,
        CancellationToken cancellationToken)
    {
        if (!_myInfoService.IsConfigured)
            return BadRequest(new { message = "MyInfo/Singpass is not configured" });

        var cacheKey = GetStateCacheKey(request.State);
        var cachedSession = await _distributedCache.GetStringAsync(cacheKey, cancellationToken);
        if (string.IsNullOrWhiteSpace(cachedSession))
            return BadRequest(new { message = "MyInfo state is invalid or has already been used" });

        var authSession = JsonSerializer.Deserialize<MyInfoAuthSessionState>(cachedSession);
        if (authSession is null || !string.Equals(authSession.StateId, request.State, StringComparison.Ordinal))
            return BadRequest(new { message = "MyInfo state is invalid or has already been used" });

        if (DateTimeHelper.UtcOffsetNow - authSession.IssuedAtUtc > MyInfoStateLifetime)
        {
            await _distributedCache.RemoveAsync(cacheKey, cancellationToken);
            return BadRequest(new { message = "MyInfo state has expired" });
        }

        await _distributedCache.RemoveAsync(cacheKey, cancellationToken);

        var personData = await _myInfoService.GetPersonDataAsync(
            request.AuthCode,
            authSession.CodeVerifier,
            authSession.Nonce,
            authSession.DpopPrivateKey,
            cancellationToken);

        return Ok(personData);
    }

    [HttpGet]
    [RequireAccessFunction(AccessFunctionCodes.Api.MyInfoUse)]
    public IActionResult IsConfigured()
    {
        return Ok(new { configured = _myInfoService.IsConfigured });
    }

    [HttpPost]
    [RequireAccessFunction(AccessFunctionCodes.Api.MyInfoUse)]
    public async Task<ActionResult<DataTablePageDto<MyInfoTestProfileDto>>> SearchTestProfiles(
        [FromBody] DataTableRequestDto request,
        CancellationToken cancellationToken)
    {
        var profiles = ApplyTestProfileQuery(await LoadTestProfilesAsync(cancellationToken), request);
        profiles = ApplyTestProfileSort(profiles, request);
        var totalCount = profiles.Count;
        return Ok(new DataTablePageDto<MyInfoTestProfileDto>
        {
            Items = profiles.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
        });
    }

    [HttpPost]
    [RequireAccessFunction(AccessFunctionCodes.Api.MyInfoUse)]
    public async Task<ActionResult<DataTableFilterOptionPageDto>> GetTestProfileFilterOptions(
        [FromBody] DataTableFilterOptionsRequestDto request,
        CancellationToken cancellationToken)
    {
        var profiles = ApplyTestProfileQuery(await LoadTestProfilesAsync(cancellationToken), request, request.ColumnKey);
        var values = profiles
            .Select(profile => GetTestProfileValue(profile, request.ColumnKey))
            .Where(value => !string.IsNullOrWhiteSpace(value));
        if (!string.IsNullOrWhiteSpace(request.OptionSearch))
        {
            values = values.Where(value => value.Contains(request.OptionSearch.Trim(), StringComparison.OrdinalIgnoreCase));
        }
        var grouped = values
            .GroupBy(value => value, StringComparer.OrdinalIgnoreCase)
            .Select(group => new DataTableFilterOptionDto
            {
                Label = group.Key,
                Value = group.Key,
                Count = group.Count(),
            })
            .OrderBy(option => option.Label, StringComparer.OrdinalIgnoreCase)
            .ThenBy(option => option.Value, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return Ok(new DataTableFilterOptionPageDto
        {
            Items = grouped.Skip((request.OptionPage - 1) * request.OptionPageSize).Take(request.OptionPageSize).ToList(),
            TotalCount = grouped.Count,
            Page = request.OptionPage,
            PageSize = request.OptionPageSize,
        });
    }

    private async Task<List<MyInfoTestProfileDto>> LoadTestProfilesAsync(CancellationToken cancellationToken)
    {
        await using var stream = System.IO.File.OpenRead(_testProfilesPath);
        return await JsonSerializer.DeserializeAsync<List<MyInfoTestProfileDto>>(
            stream,
            JsonSerializerOptions.Web,
            cancellationToken) ?? [];
    }

    private static List<MyInfoTestProfileDto> ApplyTestProfileQuery(
        IEnumerable<MyInfoTestProfileDto> source,
        DataTableRequestDto request,
        string? excludedFilter = null)
    {
        var query = source;
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(profile => TestProfileValues(profile).Any(value => value.Contains(search, StringComparison.OrdinalIgnoreCase)));
        }
        foreach (var filter in request.Filters.Where(filter => !filter.Key.Equals(excludedFilter, StringComparison.OrdinalIgnoreCase)))
        {
            var values = filter.Values.ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (values.Count > 0)
            {
                query = query.Where(profile => values.Contains(GetTestProfileValue(profile, filter.Key)));
            }
        }
        return query.ToList();
    }

    private static List<MyInfoTestProfileDto> ApplyTestProfileSort(
        List<MyInfoTestProfileDto> profiles,
        DataTableRequestDto request)
    {
        IOrderedEnumerable<MyInfoTestProfileDto>? ordered = null;
        foreach (var sort in request.GetEffectiveSorts())
        {
            var key = sort.Key;
            var descending = string.Equals(sort.Direction, "desc", StringComparison.OrdinalIgnoreCase);
            ordered = ordered is null
                ? descending
                    ? profiles.OrderByDescending(profile => GetTestProfileValue(profile, key), StringComparer.OrdinalIgnoreCase)
                    : profiles.OrderBy(profile => GetTestProfileValue(profile, key), StringComparer.OrdinalIgnoreCase)
                : descending
                    ? ordered.ThenByDescending(profile => GetTestProfileValue(profile, key), StringComparer.OrdinalIgnoreCase)
                    : ordered.ThenBy(profile => GetTestProfileValue(profile, key), StringComparer.OrdinalIgnoreCase);
        }

        return (ordered ?? profiles.OrderBy(profile => profile.Uinfin, StringComparer.OrdinalIgnoreCase))
            .ThenBy(profile => profile.Uinfin, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IEnumerable<string> TestProfileValues(MyInfoTestProfileDto profile) =>
        [profile.Uinfin, profile.Name, profile.Sex, profile.Race, profile.Dob, profile.Nationality, profile.Email, profile.Mobile, profile.PassType, profile.PostalCode];

    private static string GetTestProfileValue(MyInfoTestProfileDto profile, string key) =>
        key.ToLowerInvariant() switch
        {
            "uinfin" => profile.Uinfin,
            "name" => profile.Name,
            "sex" => profile.Sex,
            "race" => profile.Race,
            "dob" => profile.Dob,
            "nationality" => profile.Nationality,
            "email" => profile.Email,
            "mobile" => profile.Mobile,
            "passtype" => profile.PassType,
            "postalcode" => profile.PostalCode,
            _ => string.Empty,
        };

    private static string GetStateCacheKey(string stateId)
    {
        return $"{MyInfoStateCachePrefix}{stateId}";
    }
}

public record MyInfoCallbackRequest(string AuthCode, string State);

public record MyInfoAuthSessionState(
    string StateId,
    string CodeVerifier,
    string Nonce,
    string DpopPrivateKey,
    DateTimeOffset IssuedAtUtc);
