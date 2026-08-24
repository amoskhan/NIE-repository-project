namespace Domain.Models;

/// <summary>
/// Stores one application-scoped default data-table view for a signed-in user.
/// The JSON payload is validated at the API boundary and normalized by the
/// current table definition before it is applied.
/// </summary>
public sealed class UserDataTablePreference : TimestampedEntity
{
    public Guid ApplicationId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string TableKey { get; set; } = string.Empty;
    public int DefinitionVersion { get; set; } = 1;
    public string PreferencesJson { get; set; } = "{}";
    public int Revision { get; set; } = 1;
}
