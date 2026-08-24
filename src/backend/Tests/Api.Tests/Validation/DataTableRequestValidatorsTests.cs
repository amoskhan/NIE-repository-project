using Api.Validation;
using Application.Contracts;
using FluentValidation.Results;

namespace Api.Tests.Validation;

/// <summary>
/// Data-table requests are attacker-controlled and drive dynamic SQL projections,
/// so the bounds on sorts, filters and search text are security-relevant.
/// </summary>
public sealed class DataTableRequestValidatorsTests
{
    private readonly DataTableRequestDtoValidator _validator = new();

    [Fact]
    public void A_default_request_is_accepted()
    {
        Assert.True(_validator.Validate(new DataTableRequestDto()).IsValid);
    }

    [Theory]
    [InlineData("asc")]
    [InlineData("desc")]
    [InlineData("ASC")]
    [InlineData("Desc")]
    public void Sort_direction_accepts_both_directions_in_any_case(string direction)
    {
        var request = new DataTableRequestDto { SortDirection = direction };

        Assert.True(_validator.Validate(request).IsValid);
    }

    [Theory]
    [InlineData("ascending")]
    [InlineData("")]
    [InlineData("asc;drop table")]
    public void Sort_direction_rejects_anything_else(string direction)
    {
        var request = new DataTableRequestDto { SortDirection = direction };

        AssertInvalid(_validator.Validate(request), nameof(DataTableRequestDto.SortDirection));
    }

    [Fact]
    public void Search_text_is_capped_at_200_characters()
    {
        Assert.True(_validator.Validate(new DataTableRequestDto { Search = new string('a', 200) }).IsValid);
        AssertInvalid(
            _validator.Validate(new DataTableRequestDto { Search = new string('a', 201) }),
            nameof(DataTableRequestDto.Search));
    }

    [Fact]
    public void At_most_five_ordered_sorts_are_allowed()
    {
        Assert.True(_validator.Validate(RequestWithSorts(5)).IsValid);
        AssertInvalid(_validator.Validate(RequestWithSorts(6)), nameof(DataTableRequestDto.Sorts));
    }

    [Fact]
    public void The_same_column_cannot_be_sorted_twice_even_in_a_different_case()
    {
        var request = new DataTableRequestDto
        {
            Sorts =
            [
                new DataTableSortDto { Key = "createdOn", Direction = "asc" },
                new DataTableSortDto { Key = "CREATEDON", Direction = "desc" },
            ],
        };

        AssertInvalid(_validator.Validate(request), nameof(DataTableRequestDto.Sorts));
    }

    [Fact]
    public void A_sort_entry_must_name_a_column()
    {
        var request = new DataTableRequestDto
        {
            Sorts = [new DataTableSortDto { Key = string.Empty, Direction = "asc" }],
        };

        Assert.False(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void At_most_twenty_column_filters_are_allowed()
    {
        Assert.True(_validator.Validate(RequestWithFilters(20)).IsValid);
        AssertInvalid(_validator.Validate(RequestWithFilters(21)), nameof(DataTableRequestDto.Filters));
    }

    [Fact]
    public void A_single_filter_may_not_select_more_than_a_hundred_values()
    {
        var acceptable = FilterWithValues(100);
        var excessive = FilterWithValues(101);

        Assert.True(_validator.Validate(new DataTableRequestDto { Filters = [acceptable] }).IsValid);
        Assert.False(_validator.Validate(new DataTableRequestDto { Filters = [excessive] }).IsValid);
    }

    [Fact]
    public void Filter_values_are_individually_bounded()
    {
        var request = new DataTableRequestDto
        {
            Filters = [new DataTableFilterDto { Key = "status", Values = [new string('x', 201)] }],
        };

        Assert.False(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void An_empty_filter_value_is_rejected()
    {
        var request = new DataTableRequestDto
        {
            Filters = [new DataTableFilterDto { Key = "status", Values = ["Approved", string.Empty] }],
        };

        Assert.False(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void Filter_options_requests_must_name_the_column_being_expanded()
    {
        var validator = new DataTableFilterOptionsRequestDtoValidator();

        AssertInvalid(
            validator.Validate(new DataTableFilterOptionsRequestDto()),
            nameof(DataTableFilterOptionsRequestDto.ColumnKey));
        Assert.True(validator.Validate(new DataTableFilterOptionsRequestDto { ColumnKey = "status" }).IsValid);
    }

    [Fact]
    public void Filter_options_requests_inherit_the_base_request_bounds()
    {
        var validator = new DataTableFilterOptionsRequestDtoValidator();
        var request = new DataTableFilterOptionsRequestDto
        {
            ColumnKey = "status",
            SortDirection = "sideways",
        };

        AssertInvalid(validator.Validate(request), nameof(DataTableRequestDto.SortDirection));
    }

    [Fact]
    public void The_option_search_term_is_capped()
    {
        var validator = new DataTableFilterOptionsRequestDtoValidator();
        var request = new DataTableFilterOptionsRequestDto
        {
            ColumnKey = "status",
            OptionSearch = new string('a', 201),
        };

        AssertInvalid(
            validator.Validate(request),
            nameof(DataTableFilterOptionsRequestDto.OptionSearch));
    }

    private static DataTableRequestDto RequestWithSorts(int count) =>
        new()
        {
            Sorts = Enumerable
                .Range(0, count)
                .Select(index => new DataTableSortDto { Key = $"column{index}", Direction = "asc" })
                .ToList(),
        };

    private static DataTableRequestDto RequestWithFilters(int count) =>
        new()
        {
            Filters = Enumerable
                .Range(0, count)
                .Select(index => new DataTableFilterDto { Key = $"column{index}", Values = ["value"] })
                .ToList(),
        };

    private static DataTableFilterDto FilterWithValues(int count) =>
        new()
        {
            Key = "status",
            Values = Enumerable.Range(0, count).Select(index => $"value{index}").ToList(),
        };

    private static void AssertInvalid(ValidationResult result, string propertyName)
    {
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == propertyName);
    }
}
