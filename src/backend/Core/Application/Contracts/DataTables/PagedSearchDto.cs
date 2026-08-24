namespace Application.Contracts;

/// <summary>
/// Base class for any search DTO that supports pagination.
/// PageSize is clamped to [1, MaxPageSize] on set so a malicious or buggy caller cannot
/// request unbounded result sets. Every search DTO must extend this — see template
/// rule N-17 / OWASP API4.
/// </summary>
public abstract class PagedSearchDto
{
    public const int MaxPageSize = 100;
    public const int DefaultPageSize = 25;

    private int _page = 1;
    private int _pageSize = DefaultPageSize;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => value
        };
    }
}
