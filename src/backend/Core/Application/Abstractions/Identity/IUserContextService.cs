namespace Application.Abstractions.Identity;

/// <summary>
/// Interface for accessing current user context information.
/// Used for audit fields (CreatedBy, UpdatedBy) in entities.
/// </summary>
public interface IUserContextService
{
    /// <summary>
    /// Gets the current user's ID.
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// Gets the current user's name.
    /// </summary>
    string? UserName { get; }

    /// <summary>
    /// Gets the current user's email.
    /// </summary>
    string? UserEmail { get; }

    /// <summary>
    /// Gets the current session ID.
    /// </summary>
    string? SessionId { get; }
}
