namespace Application.Contracts;

/// <summary>
/// One whitelisted data-table filter and its selected values.
/// </summary>
public sealed class DataTableFilterDto
{
    public string Key { get; set; } = string.Empty;
    public List<string> Values { get; set; } = [];
}

/// <summary>
/// One ordered, whitelisted sort applied by a data-table endpoint.
/// </summary>
public sealed class DataTableSortDto
{
    public string Key { get; set; } = string.Empty;
    public string Direction { get; set; } = "asc";
}

/// <summary>
/// Bounded request shared by live data-grid endpoints.
/// Feature queries must whitelist every search, sort, and filter field.
/// </summary>
public class DataTableRequestDto : PagedSearchDto
{
    public string? Search { get; set; }
    public string? SortBy { get; set; }
    public string SortDirection { get; set; } = "asc";
    public List<DataTableSortDto> Sorts { get; set; } = [];
    public List<DataTableFilterDto> Filters { get; set; } = [];

    public IReadOnlyList<DataTableSortDto> GetEffectiveSorts()
    {
        if (Sorts.Count > 0)
        {
            return Sorts;
        }

        return string.IsNullOrWhiteSpace(SortBy)
            ? []
            : [new DataTableSortDto { Key = SortBy.Trim(), Direction = SortDirection }];
    }

    public IReadOnlyList<string> GetFilterValues(string key) =>
        Filters
            .Where(filter => string.Equals(filter.Key, key, StringComparison.OrdinalIgnoreCase))
            .SelectMany(filter => filter.Values)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
}

/// <summary>
/// Request for one page of distinct values for a whitelisted column.
/// </summary>
public sealed class DataTableFilterOptionsRequestDto : DataTableRequestDto
{
    private int _optionPage = 1;
    private int _optionPageSize = DefaultPageSize;

    public string ColumnKey { get; set; } = string.Empty;
    public string? OptionSearch { get; set; }

    public int OptionPage
    {
        get => _optionPage;
        set => _optionPage = value < 1 ? 1 : value;
    }

    public int OptionPageSize
    {
        get => _optionPageSize;
        set => _optionPageSize = value switch
        {
            < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => value,
        };
    }
}

/// <summary>
/// One distinct filter value and the number of matching records.
/// </summary>
public sealed class DataTableFilterOptionDto
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int Count { get; set; }
}

/// <summary>
/// Standard page returned by every live data-grid endpoint.
/// </summary>
public class DataTablePageDto<T>
{
    public List<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}

public sealed class DataTableFilterOptionPageDto : DataTablePageDto<DataTableFilterOptionDto>;
