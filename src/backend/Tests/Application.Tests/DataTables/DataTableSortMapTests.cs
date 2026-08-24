using Application.Contracts;
using Application.Features.DataTable;

namespace Application.Tests;

public sealed class DataTableSortMapTests
{
    private sealed record Row(string Id, string VendorName, decimal TotalAmount);

    private static readonly Row Alpha = new("1", "Alpha", 300m);
    private static readonly Row Bravo = new("2", "Bravo", 100m);
    private static readonly Row Charlie = new("3", "Alpha", 200m);

    private static DataTableSortMap<Row> CreateMap() =>
        new DataTableSortMap<Row>()
            .Add("vendorName", row => row.VendorName)
            .Add("totalAmount", row => row.TotalAmount);

    private static IQueryable<Row> CreateRows() =>
        new[] { Bravo, Alpha, Charlie }.AsQueryable();

    private static List<string> Apply(DataTableRequestDto request) =>
        CreateMap()
            .Apply(CreateRows(), request, query => query.OrderBy(row => row.Id), row => row.Id)
            .Select(row => row.Id)
            .ToList();

    [Fact]
    public void Ignores_a_sort_key_that_is_not_whitelisted()
    {
        var request = new DataTableRequestDto
        {
            Sorts = [new DataTableSortDto { Key = "password", Direction = "asc" }],
        };

        Assert.Equal(["1", "2", "3"], Apply(request));
    }

    [Fact]
    public void Falls_back_to_the_default_order_when_no_sort_is_requested()
    {
        Assert.Equal(["1", "2", "3"], Apply(new DataTableRequestDto()));
    }

    [Fact]
    public void Applies_a_whitelisted_ascending_sort()
    {
        var request = new DataTableRequestDto
        {
            Sorts = [new DataTableSortDto { Key = "totalAmount", Direction = "asc" }],
        };

        Assert.Equal(["2", "3", "1"], Apply(request));
    }

    [Fact]
    public void Applies_a_whitelisted_descending_sort()
    {
        var request = new DataTableRequestDto
        {
            Sorts = [new DataTableSortDto { Key = "totalAmount", Direction = "desc" }],
        };

        Assert.Equal(["1", "3", "2"], Apply(request));
    }

    [Theory]
    [InlineData("DESC")]
    [InlineData("Desc")]
    public void Reads_the_descending_direction_without_regard_to_case(string direction)
    {
        var request = new DataTableRequestDto
        {
            Sorts = [new DataTableSortDto { Key = "totalAmount", Direction = direction }],
        };

        Assert.Equal(["1", "3", "2"], Apply(request));
    }

    [Theory]
    [InlineData("descending")]
    [InlineData("")]
    [InlineData("anything-else")]
    public void Treats_an_unrecognised_direction_as_ascending(string direction)
    {
        var request = new DataTableRequestDto
        {
            Sorts = [new DataTableSortDto { Key = "totalAmount", Direction = direction }],
        };

        Assert.Equal(["2", "3", "1"], Apply(request));
    }

    [Fact]
    public void Chains_secondary_sorts_in_the_requested_order()
    {
        var request = new DataTableRequestDto
        {
            Sorts =
            [
                new DataTableSortDto { Key = "vendorName", Direction = "asc" },
                new DataTableSortDto { Key = "totalAmount", Direction = "desc" },
            ],
        };

        Assert.Equal(["1", "3", "2"], Apply(request));
    }

    [Fact]
    public void Skips_only_the_unknown_key_when_a_request_mixes_known_and_unknown_sorts()
    {
        var request = new DataTableRequestDto
        {
            Sorts =
            [
                new DataTableSortDto { Key = "internalCost", Direction = "asc" },
                new DataTableSortDto { Key = "totalAmount", Direction = "desc" },
            ],
        };

        Assert.Equal(["1", "3", "2"], Apply(request));
    }

    [Fact]
    public void Matches_whitelisted_sort_keys_without_regard_to_case()
    {
        var request = new DataTableRequestDto { SortBy = "TOTALAMOUNT", SortDirection = "desc" };

        Assert.Equal(["1", "3", "2"], Apply(request));
    }

    [Fact]
    public void Uses_the_legacy_single_sort_when_the_sort_list_is_empty()
    {
        var request = new DataTableRequestDto { SortBy = "  vendorName  ", SortDirection = "desc" };

        Assert.Equal(["2", "1", "3"], Apply(request));
    }

    [Fact]
    public void Breaks_ties_with_the_stable_key_so_paging_cannot_repeat_rows()
    {
        var request = new DataTableRequestDto
        {
            Sorts = [new DataTableSortDto { Key = "vendorName", Direction = "asc" }],
        };

        Assert.Equal(["1", "3", "2"], Apply(request));
    }

    [Fact]
    public void Lets_a_later_registration_replace_an_earlier_selector_for_the_same_key()
    {
        var map = new DataTableSortMap<Row>()
            .Add("sortKey", row => row.VendorName)
            .Add("sortKey", row => row.TotalAmount);
        var request = new DataTableRequestDto
        {
            Sorts = [new DataTableSortDto { Key = "sortKey", Direction = "asc" }],
        };

        var ordered = map
            .Apply(CreateRows(), request, query => query.OrderBy(row => row.Id), row => row.Id)
            .Select(row => row.Id)
            .ToList();

        Assert.Equal(["2", "3", "1"], ordered);
    }
}
