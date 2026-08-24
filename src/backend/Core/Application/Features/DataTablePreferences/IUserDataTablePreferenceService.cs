using Application.Contracts;

namespace Application.Features.DataTablePreferences;

public interface IUserDataTablePreferenceService
{
    Task<IReadOnlyList<UserDataTablePreferenceDto>> GetAllAsync(
        Guid applicationId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<UserDataTablePreferenceDto?> GetAsync(
        Guid applicationId,
        string userId,
        string tableKey,
        CancellationToken cancellationToken = default);

    Task<UserDataTablePreferenceDto> UpsertAsync(
        Guid applicationId,
        string userId,
        string tableKey,
        UpsertUserDataTablePreferenceDto request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid applicationId,
        string userId,
        string tableKey,
        CancellationToken cancellationToken = default);
}

public sealed class DataTablePreferenceConflictException : Exception
{
    public DataTablePreferenceConflictException()
        : base("The table preference was changed in another session. Reload it and try again.")
    {
    }
}

public static class DataTablePreferenceTableKey
{
    public static bool TryNormalize(string? value, out string normalized)
    {
        normalized = value?.Trim().ToLowerInvariant() ?? string.Empty;
        return normalized.Length is >= 3 and <= 160 &&
            char.IsLetterOrDigit(normalized[0]) &&
            char.IsLetterOrDigit(normalized[^1]) &&
            normalized.All(character =>
                char.IsLetterOrDigit(character) || character is '.' or '-');
    }

    public static string Normalize(string? value)
    {
        if (!TryNormalize(value, out var normalized))
        {
            throw new ArgumentException(
                "A table key must be 3 to 160 lowercase letters, numbers, dots, or hyphens.",
                nameof(value));
        }

        return normalized;
    }
}
