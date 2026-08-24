using Application.Contracts;
using Application.Features.DataTable;
using Application.Features.DataTablePreferences;

namespace Architecture.Tests;

public class DataTableContractTests
{
    private sealed record SortableRow(Guid Id, string Group, int Sequence);

    [Fact]
    public void Data_table_queries_support_ordered_multi_column_sorting()
    {
        var sortsProperty = typeof(DataTableRequestDto).GetProperty("Sorts");

        Assert.NotNull(sortsProperty);
        Assert.True(typeof(System.Collections.IEnumerable).IsAssignableFrom(sortsProperty!.PropertyType));
    }

    [Fact]
    public void Data_table_sort_map_applies_all_requested_sorts_in_priority_order()
    {
        var rows = new[]
        {
            new SortableRow(Guid.Parse("00000000-0000-0000-0000-000000000003"), "B", 1),
            new SortableRow(Guid.Parse("00000000-0000-0000-0000-000000000002"), "A", 2),
            new SortableRow(Guid.Parse("00000000-0000-0000-0000-000000000001"), "A", 1),
        }.AsQueryable();
        var request = new DataTableRequestDto
        {
            Sorts =
            [
                new DataTableSortDto { Key = "group", Direction = "asc" },
                new DataTableSortDto { Key = "sequence", Direction = "desc" },
            ],
        };

        var ordered = new DataTableSortMap<SortableRow>()
            .Add("group", row => row.Group)
            .Add("sequence", row => row.Sequence)
            .Apply(rows, request, query => query.OrderBy(row => row.Id), row => row.Id)
            .ToList();

        Assert.Equal([2, 1, 1], ordered.Select(row => row.Sequence));
        Assert.Equal(["A", "A", "B"], ordered.Select(row => row.Group));
    }

    [Theory]
    [InlineData(" Procurement.Vendors ", "procurement.vendors")]
    [InlineData("administration.audit-logs", "administration.audit-logs")]
    public void Preference_table_keys_are_normalized_to_a_stable_safe_scope(
        string value,
        string expected)
    {
        Assert.True(DataTablePreferenceTableKey.TryNormalize(value, out var normalized));
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("procurement/vendors")]
    [InlineData(".vendors")]
    [InlineData("vendors.")]
    public void Preference_table_keys_reject_ambiguous_or_unsafe_values(string value)
    {
        Assert.False(DataTablePreferenceTableKey.TryNormalize(value, out _));
    }

    [Fact]
    public void User_data_table_preferences_are_part_of_the_domain_and_persistence_boundary()
    {
        var preferenceType = typeof(Domain.Models.BaseEntity).Assembly
            .GetType("Domain.Models.UserDataTablePreference");
        var preferenceSet = typeof(Application.Abstractions.IApplicationDbContext)
            .GetProperty("UserDataTablePreferences");

        Assert.NotNull(preferenceType);
        Assert.NotNull(preferenceSet);
        Assert.Equal(typeof(Guid), preferenceType!.GetProperty("Id")!.PropertyType);
    }

    [Fact]
    public void User_data_table_preferences_have_a_typed_application_service_contract()
    {
        var responseContract = typeof(DataTableRequestDto).Assembly
            .GetType("Application.Contracts.UserDataTablePreferenceDto");
        var serviceContract = typeof(Application.Features.IApplicationService).Assembly
            .GetType("Application.Features.DataTablePreferences.IUserDataTablePreferenceService");

        Assert.NotNull(responseContract);
        Assert.NotNull(serviceContract);
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(0, 1)]
    [InlineData(2, 2)]
    public void Data_table_page_is_never_below_one(int requested, int expected)
    {
        var request = new DataTableRequestDto { Page = requested };

        Assert.Equal(expected, request.Page);
    }

    [Theory]
    [InlineData(-1, 25)]
    [InlineData(0, 25)]
    [InlineData(40, 40)]
    [InlineData(101, 100)]
    public void Data_table_page_size_is_bounded(int requested, int expected)
    {
        var request = new DataTableRequestDto { PageSize = requested };

        Assert.Equal(expected, request.PageSize);
    }

    [Fact]
    public void Filter_option_page_size_is_bounded_independently()
    {
        var request = new DataTableFilterOptionsRequestDto
        {
            OptionPage = 0,
            OptionPageSize = 1_000,
        };

        Assert.Equal(1, request.OptionPage);
        Assert.Equal(100, request.OptionPageSize);
    }

    [Fact]
    public void Paged_result_uses_server_total_instead_of_current_page_count()
    {
        var result = new DataTablePageDto<string>
        {
            Items = ["one", "two"],
            TotalCount = 64,
            Page = 2,
            PageSize = 10,
        };

        Assert.Equal(7, result.TotalPages);
        Assert.True(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
    }
}
