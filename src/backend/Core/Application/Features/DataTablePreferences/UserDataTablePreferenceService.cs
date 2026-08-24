using System.Text.Json;
using Application.Abstractions;
using Application.Contracts;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.DataTablePreferences;

public sealed class UserDataTablePreferenceService : IUserDataTablePreferenceService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IApplicationDbContext _context;

    public UserDataTablePreferenceService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<UserDataTablePreferenceDto>> GetAllAsync(
        Guid applicationId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _context.UserDataTablePreferences
            .AsNoTracking()
            .Where(item => item.ApplicationId == applicationId && item.UserId == userId)
            .OrderBy(item => item.TableKey)
            .ToListAsync(cancellationToken);

        return rows.Select(ToDto).ToList();
    }

    public async Task<UserDataTablePreferenceDto?> GetAsync(
        Guid applicationId,
        string userId,
        string tableKey,
        CancellationToken cancellationToken = default)
    {
        var normalizedKey = DataTablePreferenceTableKey.Normalize(tableKey);
        var row = await _context.UserDataTablePreferences
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.ApplicationId == applicationId &&
                    item.UserId == userId &&
                    item.TableKey == normalizedKey,
                cancellationToken);
        return row is null ? null : ToDto(row);
    }

    public async Task<UserDataTablePreferenceDto> UpsertAsync(
        Guid applicationId,
        string userId,
        string tableKey,
        UpsertUserDataTablePreferenceDto request,
        CancellationToken cancellationToken = default)
    {
        var normalizedKey = DataTablePreferenceTableKey.Normalize(tableKey);
        var row = await _context.UserDataTablePreferences.SingleOrDefaultAsync(
            item => item.ApplicationId == applicationId &&
                item.UserId == userId &&
                item.TableKey == normalizedKey,
            cancellationToken);

        var isNew = row is null;
        if (row is null)
        {
            if (request.Revision is > 0)
            {
                throw new DataTablePreferenceConflictException();
            }

            row = new UserDataTablePreference
            {
                ApplicationId = applicationId,
                UserId = userId,
                TableKey = normalizedKey,
            };
            _context.UserDataTablePreferences.Add(row);
        }
        else
        {
            if (request.Revision is null || request.Revision != row.Revision)
            {
                throw new DataTablePreferenceConflictException();
            }

            row.Revision += 1;
        }

        row.DefinitionVersion = request.DefinitionVersion;
        request.Settings.FilterReminderAcknowledgedAtUtc = request.Settings.Filters.Any(
            filter => filter.Values.Count > 0)
            ? DateTimeOffset.UtcNow
            : null;
        row.PreferencesJson = JsonSerializer.Serialize(request.Settings, SerializerOptions);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new DataTablePreferenceConflictException();
        }
        catch (DbUpdateException) when (isNew)
        {
            throw new DataTablePreferenceConflictException();
        }

        return ToDto(row);
    }

    public async Task DeleteAsync(
        Guid applicationId,
        string userId,
        string tableKey,
        CancellationToken cancellationToken = default)
    {
        var normalizedKey = DataTablePreferenceTableKey.Normalize(tableKey);
        var row = await _context.UserDataTablePreferences.SingleOrDefaultAsync(
            item => item.ApplicationId == applicationId &&
                item.UserId == userId &&
                item.TableKey == normalizedKey,
            cancellationToken);
        if (row is null)
        {
            return;
        }

        _context.UserDataTablePreferences.Remove(row);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static UserDataTablePreferenceDto ToDto(UserDataTablePreference row)
    {
        try
        {
            return new UserDataTablePreferenceDto
            {
                TableKey = row.TableKey,
                DefinitionVersion = row.DefinitionVersion,
                Revision = row.Revision,
                Settings = JsonSerializer.Deserialize<DataTablePreferenceSettingsDto>(
                    row.PreferencesJson,
                    SerializerOptions) ?? new DataTablePreferenceSettingsDto(),
            };
        }
        catch (JsonException)
        {
            return new UserDataTablePreferenceDto
            {
                TableKey = row.TableKey,
                DefinitionVersion = row.DefinitionVersion,
                Revision = row.Revision,
                Settings = new DataTablePreferenceSettingsDto(),
                RepairRequired = true,
                RepairReasons =
                [
                    "The saved preference format is incompatible with this application version.",
                ],
            };
        }
    }
}
