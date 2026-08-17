using System.Text.Json;
using Auth.Models;
using Microsoft.Extensions.Caching.Distributed;
using Shared.Helpers;

namespace Auth.Services;

public interface IAuthSessionService
{
    Task<IssuedLoginResponse> IssueSessionAsync(LoginResponse loginResponse, CancellationToken cancellationToken = default);
}
