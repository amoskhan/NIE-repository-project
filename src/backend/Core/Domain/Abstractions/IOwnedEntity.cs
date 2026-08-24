namespace Domain.Abstractions;

/// <summary>
/// Marker interface for entities that have a per-record owner. The <see cref="OwnerUserId"/>
/// field is matched against <c>BaseController.UserId</c> by <c>OwnedEntityActionFilter</c> /
/// <c>BaseController.EnsureOwnedAsync&lt;T&gt;</c>. Admins (<c>BaseController.IsAdmin == true</c>)
/// bypass the check.
///
/// Use this to close OWASP API1 (Broken Object Level Authorization). Function-level
/// authorization (<c>RequireAccessFunction</c>) is necessary but not sufficient — records
/// owned by other users must still be guarded.
/// </summary>
public interface IOwnedEntity
{
    /// <summary>
    /// The user-id of the record owner. Compared against the request user's id.
    /// Must not be null/empty for the ownership check to succeed.
    /// </summary>
    string OwnerUserId { get; }
}
