using Application.Contracts;
using Application.Features.DataTablePreferences;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Integration.Tests;

public class UserDataTablePreferenceServiceTests
{
    [Fact]
    public async Task Preferences_are_user_scoped_repair_malformed_json_and_reject_stale_revisions()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var database = await PostgresTestDatabase.CreateAsync(cancellationToken);
        await using var context = database.CreateContext();
        await context.Database.EnsureCreatedAsync(cancellationToken);

        var applicationId = Guid.CreateVersion7();
        context.Applications.Add(new Domain.Models.Application
        {
            Id = applicationId,
            Name = "Preference test",
            ProjectKey = $"preference-test-{Guid.CreateVersion7():N}",
        });
        await context.SaveChangesAsync(cancellationToken);

        var service = new UserDataTablePreferenceService(context);
        var created = await service.UpsertAsync(
            applicationId,
            "staff-one",
            " Procurement.Vendors ",
            new UpsertUserDataTablePreferenceDto
            {
                DefinitionVersion = 2,
                Settings = new DataTablePreferenceSettingsDto
                {
                    PageSize = 50,
                    ColumnOrder = ["name", "code"],
                    Sorts = [new DataTableSortDto { Key = "name", Direction = "desc" }],
                    Filters =
                    [
                        new DataTablePreferenceFilterDto
                        {
                            Key = "status",
                            Values = ["active"],
                        },
                    ],
                },
            },
            cancellationToken);

        Assert.Equal("procurement.vendors", created.TableKey);
        Assert.Equal(1, created.Revision);
        Assert.NotNull(created.Settings.FilterReminderAcknowledgedAtUtc);
        Assert.InRange(
            created.Settings.FilterReminderAcknowledgedAtUtc.Value,
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow.AddMinutes(1));
        Assert.Null(await service.GetAsync(
            applicationId,
            "staff-two",
            "procurement.vendors",
            cancellationToken));

        var updated = await service.UpsertAsync(
            applicationId,
            "staff-one",
            "procurement.vendors",
            new UpsertUserDataTablePreferenceDto
            {
                DefinitionVersion = 2,
                Revision = created.Revision,
                Settings = created.Settings,
            },
            cancellationToken);
        Assert.Equal(2, updated.Revision);

        updated.Settings.Filters = [];
        var filtersRemoved = await service.UpsertAsync(
            applicationId,
            "staff-one",
            "procurement.vendors",
            new UpsertUserDataTablePreferenceDto
            {
                DefinitionVersion = 2,
                Revision = updated.Revision,
                Settings = updated.Settings,
            },
            cancellationToken);
        Assert.Null(filtersRemoved.Settings.FilterReminderAcknowledgedAtUtc);

        await Assert.ThrowsAsync<DataTablePreferenceConflictException>(() =>
            service.UpsertAsync(
                applicationId,
                "staff-one",
                "procurement.vendors",
                new UpsertUserDataTablePreferenceDto
                {
                    DefinitionVersion = 2,
                    Revision = updated.Revision,
                    Settings = created.Settings,
                },
                cancellationToken));

        // PreferencesJson is a PostgreSQL jsonb column, so the database itself
        // rejects syntactically malformed JSON. The reachable corruption in
        // production is well-formed JSON whose values no longer match the DTO,
        // which is what makes JsonSerializer.Deserialize throw JsonException.
        var row = await context.UserDataTablePreferences.SingleAsync(cancellationToken);
        row.PreferencesJson = """{"pageSize":"not-a-number"}""";
        await context.SaveChangesAsync(cancellationToken);
        var repaired = await service.GetAsync(
            applicationId,
            "staff-one",
            "procurement.vendors",
            cancellationToken);

        Assert.NotNull(repaired);
        Assert.True(repaired!.RepairRequired);
        Assert.NotEmpty(repaired.RepairReasons);
        Assert.Equal(20, repaired.Settings.PageSize);
    }
}
