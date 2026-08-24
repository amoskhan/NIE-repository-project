using Application.Contracts;
using Domain.Enums;

namespace Application.Features;

/// <summary>
/// Service for reading and evaluating access functions.
/// </summary>
public interface IAccessFunctionService
{
    /// <summary>
    /// Gets all access functions, optionally filtered by type.
    /// </summary>
    Task<List<AccessFunctionDto>> GetAllAsync(EAccessFunctionType? type = null);

    /// <summary>
    /// Gets all granted access function codes for a user.
    /// </summary>
    Task<List<string>> GetUserAccessFunctionCodesAsync(string userId);

    /// <summary>
    /// Checks whether a user has a specific access function.
    /// </summary>
    Task<bool> HasAccessAsync(string userId, string accessFunctionCode);

    /// <summary>
    /// Clears cached access evaluations for the affected users.
    /// </summary>
    Task InvalidateUsersAsync(IEnumerable<string> userIds);
}
