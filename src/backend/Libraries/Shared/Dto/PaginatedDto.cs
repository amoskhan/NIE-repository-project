namespace Shared.Dto;

/// <summary>
/// Generic paginated request DTO for list endpoints that support pagination, sorting, and filtering.
/// </summary>
public class PaginatedRequest
{
    /// <summary>
    /// Page number (1-based).
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of items per page.
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Column name to sort by.
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Sort direction: "asc" or "desc".
    /// </summary>
    public string SortDirection { get; set; } = "asc";

    /// <summary>
    /// Global search/filter text.
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Computed skip value for LINQ queries.
    /// </summary>
    public int Skip => (Page - 1) * PageSize;
}

/// <summary>
/// Generic paginated result wrapper for list endpoints.
/// </summary>
/// <typeparam name="T">The type of items in the result.</typeparam>
public class PaginatedResult<T>
{
    /// <summary>
    /// The items for the current page.
    /// </summary>
    public List<T> Items { get; set; } = new();

    /// <summary>
    /// Total number of items across all pages.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number (1-based).
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Number of items per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// Whether there is a next page.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => Page > 1;
}
