using Application.Contracts;

namespace Application.Tests;

public sealed class DataTableRequestDtoTests
{
    [Theory]
    [InlineData(-5, 1)]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(7, 7)]
    [InlineData(int.MaxValue, int.MaxValue)]
    public void Clamps_a_page_below_one_up_to_the_first_page(int requested, int expected)
    {
        var request = new DataTableRequestDto { Page = requested };

        Assert.Equal(expected, request.Page);
    }

    [Theory]
    [InlineData(-1, PagedSearchDto.DefaultPageSize)]
    [InlineData(0, PagedSearchDto.DefaultPageSize)]
    [InlineData(1, 1)]
    [InlineData(99, 99)]
    [InlineData(100, 100)]
    [InlineData(101, PagedSearchDto.MaxPageSize)]
    [InlineData(int.MaxValue, PagedSearchDto.MaxPageSize)]
    public void Bounds_the_page_size_to_the_permitted_window(int requested, int expected)
    {
        var request = new DataTableRequestDto { PageSize = requested };

        Assert.Equal(expected, request.PageSize);
    }

    [Fact]
    public void Defaults_to_the_first_page_and_the_default_page_size()
    {
        var request = new DataTableRequestDto();

        Assert.Equal(1, request.Page);
        Assert.Equal(PagedSearchDto.DefaultPageSize, request.PageSize);
    }

    [Fact]
    public void Prefers_the_multi_sort_list_over_the_legacy_single_sort()
    {
        var request = new DataTableRequestDto
        {
            SortBy = "legacy",
            SortDirection = "desc",
            Sorts =
            [
                new DataTableSortDto { Key = "vendorName", Direction = "asc" },
                new DataTableSortDto { Key = "totalAmount", Direction = "desc" },
            ],
        };

        var sorts = request.GetEffectiveSorts();

        Assert.Equal(2, sorts.Count);
        Assert.Equal("vendorName", sorts[0].Key);
        Assert.Equal("totalAmount", sorts[1].Key);
    }

    [Fact]
    public void Falls_back_to_the_legacy_single_sort_and_trims_the_key()
    {
        var request = new DataTableRequestDto { SortBy = "  poNumber  ", SortDirection = "desc" };

        var sort = Assert.Single(request.GetEffectiveSorts());

        Assert.Equal("poNumber", sort.Key);
        Assert.Equal("desc", sort.Direction);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    public void Produces_no_sort_when_neither_form_is_supplied(string? sortBy)
    {
        var request = new DataTableRequestDto { SortBy = sortBy };

        Assert.Empty(request.GetEffectiveSorts());
    }

    [Fact]
    public void Matches_filter_keys_without_regard_to_case()
    {
        var request = new DataTableRequestDto
        {
            Filters =
            [
                new DataTableFilterDto { Key = "Status", Values = ["Approved"] },
                new DataTableFilterDto { Key = "status", Values = ["Rejected"] },
            ],
        };

        var values = request.GetFilterValues("STATUS");

        Assert.Equal(2, values.Count);
        Assert.Contains("Approved", values);
        Assert.Contains("Rejected", values);
    }

    [Fact]
    public void Drops_blank_filter_values_and_trims_the_remainder()
    {
        var request = new DataTableRequestDto
        {
            Filters =
            [
                new DataTableFilterDto { Key = "status", Values = ["  Approved  ", "", "   ", "Rejected"] },
            ],
        };

        var values = request.GetFilterValues("status");

        Assert.Equal(["Approved", "Rejected"], values);
    }

    [Fact]
    public void Collapses_filter_values_that_differ_only_by_case()
    {
        var request = new DataTableRequestDto
        {
            Filters = [new DataTableFilterDto { Key = "status", Values = ["Approved", "APPROVED", "approved"] }],
        };

        Assert.Equal("Approved", Assert.Single(request.GetFilterValues("status")));
    }

    [Fact]
    public void Returns_no_values_for_a_filter_key_that_was_not_requested()
    {
        var request = new DataTableRequestDto
        {
            Filters = [new DataTableFilterDto { Key = "status", Values = ["Approved"] }],
        };

        Assert.Empty(request.GetFilterValues("vendorName"));
    }

    [Theory]
    [InlineData(-3, 1)]
    [InlineData(0, 1)]
    [InlineData(4, 4)]
    public void Clamps_the_filter_option_page(int requested, int expected)
    {
        var request = new DataTableFilterOptionsRequestDto { OptionPage = requested };

        Assert.Equal(expected, request.OptionPage);
    }

    [Theory]
    [InlineData(0, PagedSearchDto.DefaultPageSize)]
    [InlineData(-9, PagedSearchDto.DefaultPageSize)]
    [InlineData(50, 50)]
    [InlineData(100, 100)]
    [InlineData(5000, PagedSearchDto.MaxPageSize)]
    public void Bounds_the_filter_option_page_size(int requested, int expected)
    {
        var request = new DataTableFilterOptionsRequestDto { OptionPageSize = requested };

        Assert.Equal(expected, request.OptionPageSize);
    }

    [Fact]
    public void Keeps_the_option_page_independent_of_the_row_page()
    {
        var request = new DataTableFilterOptionsRequestDto { Page = 9, OptionPage = 2 };

        Assert.Equal(9, request.Page);
        Assert.Equal(2, request.OptionPage);
    }
}
