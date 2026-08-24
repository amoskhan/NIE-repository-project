using System.Linq.Expressions;
using Application.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.DataTable;

public static class DataTableQueryExtensions
{
    public static async Task<DataTablePageDto<T>> ToDataTablePageAsync<T>(
        this IQueryable<T> orderedQuery,
        DataTableRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await orderedQuery.CountAsync(cancellationToken);
        var items = await orderedQuery
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new DataTablePageDto<T>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
        };
    }

    public static async Task<DataTableFilterOptionPageDto> ToFilterOptionPageAsync(
        this IQueryable<string> values,
        DataTableFilterOptionsRequestDto request,
        Func<string, string>? labelFactory = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedValues = values.Where(value => value != "");
        if (!string.IsNullOrWhiteSpace(request.OptionSearch))
        {
            var search = request.OptionSearch.Trim().ToLower();
            normalizedValues = normalizedValues.Where(value => value.ToLower().Contains(search));
        }

        var grouped = normalizedValues
            .GroupBy(value => value)
            .Select(group => new DataTableFilterOptionDto
            {
                Value = group.Key,
                Label = group.Key,
                Count = group.Count(),
            });
        var totalCount = await grouped.CountAsync(cancellationToken);
        var items = await grouped
            .OrderBy(option => option.Label)
            .ThenBy(option => option.Value)
            .Skip((request.OptionPage - 1) * request.OptionPageSize)
            .Take(request.OptionPageSize)
            .ToListAsync(cancellationToken);

        if (labelFactory is not null)
        {
            items.ForEach(option => option.Label = labelFactory(option.Value));
        }

        return new DataTableFilterOptionPageDto
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.OptionPage,
            PageSize = request.OptionPageSize,
        };
    }
}

public sealed class DataTableSortMap<T>
{
    private interface ISortRule
    {
        IOrderedQueryable<T> ApplyFirst(IQueryable<T> query, bool descending);
        IOrderedQueryable<T> ApplyNext(IOrderedQueryable<T> query, bool descending);
    }

    private sealed class SortRule<TKey>(Expression<Func<T, TKey>> selector) : ISortRule
    {
        public IOrderedQueryable<T> ApplyFirst(IQueryable<T> query, bool descending) =>
            descending ? query.OrderByDescending(selector) : query.OrderBy(selector);

        public IOrderedQueryable<T> ApplyNext(IOrderedQueryable<T> query, bool descending) =>
            descending ? query.ThenByDescending(selector) : query.ThenBy(selector);
    }

    private readonly Dictionary<string, ISortRule> _rules = new(StringComparer.OrdinalIgnoreCase);

    public DataTableSortMap<T> Add<TKey>(string key, Expression<Func<T, TKey>> selector)
    {
        _rules[key] = new SortRule<TKey>(selector);
        return this;
    }

    public IOrderedQueryable<T> Apply<TKey>(
        IQueryable<T> query,
        DataTableRequestDto request,
        Func<IQueryable<T>, IOrderedQueryable<T>> defaultOrder,
        Expression<Func<T, TKey>> stableTieBreaker)
    {
        IOrderedQueryable<T>? ordered = null;
        foreach (var sort in request.GetEffectiveSorts())
        {
            if (!_rules.TryGetValue(sort.Key, out var rule))
            {
                continue;
            }

            var descending = string.Equals(sort.Direction, "desc", StringComparison.OrdinalIgnoreCase);
            ordered = ordered is null
                ? rule.ApplyFirst(query, descending)
                : rule.ApplyNext(ordered, descending);
        }

        return (ordered ?? defaultOrder(query)).ThenBy(stableTieBreaker);
    }
}
