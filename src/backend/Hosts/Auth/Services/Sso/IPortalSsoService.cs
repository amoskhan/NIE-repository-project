using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Auth.Models;
using BuildingBlocks.Helpers;
using Jose;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Auth.Services;

public interface IPortalSsoService
{
    Task<SsoStartResponse> StartAsync(string? returnUrl, string? callbackUrl, CancellationToken cancellationToken = default);
    Task<SsoFinalizeResult> HandleCallbackAsync(SsoCallbackRequest request, IPAddress? remoteIp, CancellationToken cancellationToken = default);
    Task<SsoFinalizeResult> FinalizeAsync(string state, CancellationToken cancellationToken = default);
}
