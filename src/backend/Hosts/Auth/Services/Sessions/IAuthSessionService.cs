using System.Text.Json;
using Auth.Models;
using BuildingBlocks.Helpers;
using Microsoft.Extensions.Caching.Distributed;

namespace Auth.Services;

public interface IAuthSessionService
{
    Task<IssuedLoginResponse> IssueSessionAsync(LoginResponse loginResponse, CancellationToken cancellationToken = default);
}
