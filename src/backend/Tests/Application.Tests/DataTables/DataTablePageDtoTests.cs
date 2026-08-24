using Application.Contracts;

namespace Application.Tests;

public sealed class DataTablePageDtoTests
{
    [Theory]
    [InlineData(0, 25, 0)]
    [InlineData(1, 25, 1)]
    [InlineData(25, 25, 1)]
    [InlineData(26, 25, 2)]
    [InlineData(99, 10, 10)]
    public void Rounds_the_total_page_count_up_to_cover_the_last_partial_page(
        int totalCount,
        int pageSize,
        int expected)
    {
        var page = new DataTablePageDto<string> { TotalCount = totalCount, PageSize = pageSize };

        Assert.Equal(expected, page.TotalPages);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Reports_no_pages_when_the_page_size_is_not_positive(int pageSize)
    {
        var page = new DataTablePageDto<string> { TotalCount = 500, PageSize = pageSize };

        Assert.Equal(0, page.TotalPages);
        Assert.False(page.HasNextPage);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    public void Offers_a_previous_page_only_after_the_first_page(int page, bool expected)
    {
        var result = new DataTablePageDto<string> { Page = page, PageSize = 10, TotalCount = 100 };

        Assert.Equal(expected, result.HasPreviousPage);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(9, true)]
    [InlineData(10, false)]
    [InlineData(11, false)]
    public void Offers_a_next_page_only_before_the_last_page(int page, bool expected)
    {
        var result = new DataTablePageDto<string> { Page = page, PageSize = 10, TotalCount = 100 };

        Assert.Equal(expected, result.HasNextPage);
    }

    [Fact]
    public void Reports_no_navigation_for_an_empty_result_set()
    {
        var page = new DataTablePageDto<string> { Page = 1, PageSize = 25, TotalCount = 0 };

        Assert.Empty(page.Items);
        Assert.Equal(0, page.TotalPages);
        Assert.False(page.HasNextPage);
        Assert.False(page.HasPreviousPage);
    }
}
