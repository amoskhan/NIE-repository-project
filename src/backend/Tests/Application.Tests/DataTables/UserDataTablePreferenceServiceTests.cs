using System.Text.Json;
using Application.Abstractions;
using Application.Contracts;
using Application.Features.DataTablePreferences;
using Application.Tests.TestSupport;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Application.Tests;

public sealed class UserDataTablePreferenceServiceTests
{
    private static readonly Guid ApplicationId = Guid.Parse("8f3c2b6a-0a4d-4a2f-9d1e-6f7c5b4a3d21");
    private static readonly Guid OtherApplicationId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private const string UserId = "staff-1";
    private const string TableKey = "procurement.purchase-order.list";

    private static (UserDataTablePreferenceService Service,
        IApplicationDbContext Context,
        FakeDbSet<UserDataTablePreference> Set) CreateService(params UserDataTablePreference[] rows)
    {
        var set = new FakeDbSet<UserDataTablePreference>(rows);
        var context = Substitute.For<IApplicationDbContext>();
        context.UserDataTablePreferences.Returns(set);
        return (new UserDataTablePreferenceService(context), context, set);
    }

    private static UserDataTablePreference ExistingRow(
        int revision = 3,
        string? preferencesJson = null,
        string tableKey = TableKey,
        string userId = UserId,
        Guid? applicationId = null) =>
        new()
        {
            ApplicationId = applicationId ?? ApplicationId,
            UserId = userId,
            TableKey = tableKey,
            DefinitionVersion = 1,
            Revision = revision,
            PreferencesJson = preferencesJson ?? """{"pageSize":20,"density":"comfortable"}""",
        };

    private static UpsertUserDataTablePreferenceDto Request(
        int? revision,
        DataTablePreferenceSettingsDto? settings = null,
        int definitionVersion = 1) =>
        new()
        {
            DefinitionVersion = definitionVersion,
            Revision = revision,
            Settings = settings ?? new DataTablePreferenceSettingsDto(),
        };

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    public async Task Upsert_rejects_a_revision_for_a_table_that_has_no_saved_preference(int revision)
    {
        var (service, context, set) = CreateService();

        await Assert.ThrowsAsync<DataTablePreferenceConflictException>(async () =>
            await service.UpsertAsync(
                ApplicationId,
                UserId,
                TableKey,
                Request(revision),
                TestContext.Current.CancellationToken));

        Assert.Empty(set.Added);
        await context.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Upsert_creates_the_first_preference_when_no_positive_revision_is_supplied(int? revision)
    {
        var (service, context, set) = CreateService();

        var result = await service.UpsertAsync(
            ApplicationId,
            UserId,
            TableKey,
            Request(revision),
            TestContext.Current.CancellationToken);

        var added = Assert.Single(set.Added);
        Assert.Equal(TableKey, added.TableKey);
        Assert.Equal(ApplicationId, added.ApplicationId);
        Assert.Equal(UserId, added.UserId);
        Assert.Equal(1, result.Revision);
        await context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Upsert_rejects_a_missing_revision_for_an_existing_preference()
    {
        var (service, context, _) = CreateService(ExistingRow(revision: 3));

        await Assert.ThrowsAsync<DataTablePreferenceConflictException>(async () =>
            await service.UpsertAsync(
                ApplicationId,
                UserId,
                TableKey,
                Request(revision: null),
                TestContext.Current.CancellationToken));

        await context.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(0)]
    public async Task Upsert_rejects_a_revision_that_does_not_match_the_saved_row(int revision)
    {
        var row = ExistingRow(revision: 3);
        var (service, _, _) = CreateService(row);

        await Assert.ThrowsAsync<DataTablePreferenceConflictException>(async () =>
            await service.UpsertAsync(
                ApplicationId,
                UserId,
                TableKey,
                Request(revision),
                TestContext.Current.CancellationToken));

        Assert.Equal(3, row.Revision);
    }

    [Fact]
    public async Task Upsert_increments_the_revision_when_the_supplied_revision_matches()
    {
        var row = ExistingRow(revision: 3);
        var (service, context, set) = CreateService(row);
        var settings = new DataTablePreferenceSettingsDto { PageSize = 50, Density = "compact" };

        var result = await service.UpsertAsync(
            ApplicationId,
            UserId,
            TableKey,
            Request(revision: 3, settings, definitionVersion: 4),
            TestContext.Current.CancellationToken);

        Assert.Empty(set.Added);
        Assert.Equal(4, row.Revision);
        Assert.Equal(4, result.Revision);
        Assert.Equal(4, result.DefinitionVersion);
        Assert.Equal(50, result.Settings.PageSize);
        Assert.Equal("compact", result.Settings.Density);
        await context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Upsert_stamps_the_filter_reminder_when_a_filter_carries_values()
    {
        var (service, _, _) = CreateService();
        var settings = new DataTablePreferenceSettingsDto
        {
            Filters = [new DataTablePreferenceFilterDto { Key = "status", Values = ["Approved"] }],
        };

        var result = await service.UpsertAsync(
            ApplicationId,
            UserId,
            TableKey,
            Request(revision: null, settings),
            TestContext.Current.CancellationToken);

        Assert.NotNull(result.Settings.FilterReminderAcknowledgedAtUtc);
    }

    [Fact]
    public async Task Upsert_clears_a_client_supplied_filter_reminder_when_no_filter_has_values()
    {
        var (service, _, _) = CreateService();
        var settings = new DataTablePreferenceSettingsDto
        {
            FilterReminderAcknowledgedAtUtc = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero),
            Filters = [new DataTablePreferenceFilterDto { Key = "status", Values = [] }],
        };

        var result = await service.UpsertAsync(
            ApplicationId,
            UserId,
            TableKey,
            Request(revision: null, settings),
            TestContext.Current.CancellationToken);

        Assert.Null(result.Settings.FilterReminderAcknowledgedAtUtc);
    }

    [Fact]
    public async Task Upsert_clears_the_filter_reminder_when_there_are_no_filters_at_all()
    {
        var (service, _, _) = CreateService();
        var settings = new DataTablePreferenceSettingsDto
        {
            FilterReminderAcknowledgedAtUtc = DateTimeOffset.UtcNow,
        };

        var result = await service.UpsertAsync(
            ApplicationId,
            UserId,
            TableKey,
            Request(revision: null, settings),
            TestContext.Current.CancellationToken);

        Assert.Null(result.Settings.FilterReminderAcknowledgedAtUtc);
    }

    [Fact]
    public async Task Upsert_normalises_the_table_key_before_matching_the_saved_row()
    {
        var row = ExistingRow(revision: 2);
        var (service, _, set) = CreateService(row);

        var result = await service.UpsertAsync(
            ApplicationId,
            UserId,
            "   PROCUREMENT.Purchase-Order.LIST   ",
            Request(revision: 2),
            TestContext.Current.CancellationToken);

        Assert.Empty(set.Added);
        Assert.Equal(3, result.Revision);
        Assert.Equal(TableKey, result.TableKey);
    }

    [Fact]
    public async Task Upsert_rejects_a_table_key_outside_the_allowed_shape()
    {
        var (service, context, _) = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await service.UpsertAsync(
                ApplicationId,
                UserId,
                "no spaces allowed",
                Request(revision: null),
                TestContext.Current.CancellationToken));

        await context.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Upsert_translates_a_concurrency_failure_into_a_conflict()
    {
        var (service, context, _) = CreateService(ExistingRow(revision: 3));
        context.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<int>(_ => throw new DbUpdateConcurrencyException());

        await Assert.ThrowsAsync<DataTablePreferenceConflictException>(async () =>
            await service.UpsertAsync(
                ApplicationId,
                UserId,
                TableKey,
                Request(revision: 3),
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Upsert_translates_a_duplicate_insert_into_a_conflict()
    {
        var (service, context, _) = CreateService();
        context.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<int>(_ => throw new DbUpdateException("duplicate key"));

        await Assert.ThrowsAsync<DataTablePreferenceConflictException>(async () =>
            await service.UpsertAsync(
                ApplicationId,
                UserId,
                TableKey,
                Request(revision: null),
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Upsert_surfaces_an_update_failure_on_an_existing_row_unchanged()
    {
        var (service, context, _) = CreateService(ExistingRow(revision: 3));
        context.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<int>(_ => throw new DbUpdateException("constraint violated"));

        await Assert.ThrowsAsync<DbUpdateException>(async () =>
            await service.UpsertAsync(
                ApplicationId,
                UserId,
                TableKey,
                Request(revision: 3),
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Upsert_does_not_match_a_row_that_belongs_to_another_application()
    {
        var (service, _, set) = CreateService(ExistingRow(revision: 3, applicationId: OtherApplicationId));

        var result = await service.UpsertAsync(
            ApplicationId,
            UserId,
            TableKey,
            Request(revision: null),
            TestContext.Current.CancellationToken);

        Assert.Single(set.Added);
        Assert.Equal(1, result.Revision);
    }

    [Fact]
    public async Task Get_returns_nothing_when_the_user_has_no_saved_preference()
    {
        var (service, _, _) = CreateService(ExistingRow(userId: "another-user"));

        var result = await service.GetAsync(
            ApplicationId,
            UserId,
            TableKey,
            TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task Get_flags_a_saved_payload_that_cannot_be_parsed_as_needing_repair()
    {
        var (service, _, _) = CreateService(ExistingRow(preferencesJson: "{ this is not json"));

        var result = await service.GetAsync(
            ApplicationId,
            UserId,
            TableKey,
            TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.True(result.RepairRequired);
        Assert.Single(result.RepairReasons);
        Assert.Equal(20, result.Settings.PageSize);
    }

    [Fact]
    public async Task Get_falls_back_to_default_settings_for_a_null_payload_without_flagging_repair()
    {
        var (service, _, _) = CreateService(ExistingRow(preferencesJson: "null"));

        var result = await service.GetAsync(
            ApplicationId,
            UserId,
            TableKey,
            TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.False(result.RepairRequired);
        Assert.Empty(result.RepairReasons);
        Assert.Equal("comfortable", result.Settings.Density);
    }

    [Fact]
    public async Task Get_deserialises_a_stored_payload_written_with_web_naming()
    {
        var stored = JsonSerializer.Serialize(
            new DataTablePreferenceSettingsDto { PageSize = 75, HiddenColumns = ["cost"] },
            JsonSerializerOptions.Web);
        var (service, _, _) = CreateService(ExistingRow(preferencesJson: stored));

        var result = await service.GetAsync(
            ApplicationId,
            UserId,
            TableKey,
            TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(75, result.Settings.PageSize);
        Assert.Equal("cost", Assert.Single(result.Settings.HiddenColumns));
    }

    [Fact]
    public async Task Get_normalises_the_requested_table_key()
    {
        var (service, _, _) = CreateService(ExistingRow());

        var result = await service.GetAsync(
            ApplicationId,
            UserId,
            "  PROCUREMENT.PURCHASE-ORDER.LIST ",
            TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(TableKey, result.TableKey);
    }

    [Fact]
    public async Task GetAll_returns_only_the_callers_rows_ordered_by_table_key()
    {
        var (service, _, _) = CreateService(
            ExistingRow(tableKey: "vendors.list"),
            ExistingRow(tableKey: "audit.list"),
            ExistingRow(tableKey: "other-user.list", userId: "another-user"),
            ExistingRow(tableKey: "other-app.list", applicationId: OtherApplicationId));

        var result = await service.GetAllAsync(
            ApplicationId,
            UserId,
            TestContext.Current.CancellationToken);

        Assert.Equal(["audit.list", "vendors.list"], result.Select(item => item.TableKey));
    }

    [Fact]
    public async Task GetAll_returns_an_empty_list_when_nothing_is_saved()
    {
        var (service, _, _) = CreateService();

        Assert.Empty(await service.GetAllAsync(ApplicationId, UserId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Delete_does_nothing_when_the_preference_is_already_gone()
    {
        var (service, context, set) = CreateService();

        await service.DeleteAsync(ApplicationId, UserId, TableKey, TestContext.Current.CancellationToken);

        Assert.Empty(set.Removed);
        await context.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_removes_the_matching_preference_and_commits()
    {
        var row = ExistingRow();
        var (service, context, set) = CreateService(row);

        await service.DeleteAsync(ApplicationId, UserId, TableKey, TestContext.Current.CancellationToken);

        Assert.Same(row, Assert.Single(set.Removed));
        await context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_leaves_another_users_preference_untouched()
    {
        var (service, context, set) = CreateService(ExistingRow(userId: "another-user"));

        await service.DeleteAsync(ApplicationId, UserId, TableKey, TestContext.Current.CancellationToken);

        Assert.Empty(set.Removed);
        await context.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_rejects_a_table_key_outside_the_allowed_shape()
    {
        var (service, _, _) = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await service.DeleteAsync(ApplicationId, UserId, "x", TestContext.Current.CancellationToken));
    }
}
