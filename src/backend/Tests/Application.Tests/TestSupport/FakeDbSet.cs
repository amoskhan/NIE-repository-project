using System.Collections;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;

namespace Application.Tests.TestSupport;

/// <summary>
/// Minimal in-memory <see cref="DbSet{TEntity}"/> stand-in that supports the
/// asynchronous LINQ operators used by the application layer. It keeps the unit
/// tests free of any database, provider, or EF Core model.
/// </summary>
internal sealed class FakeDbSet<TEntity> : DbSet<TEntity>, IQueryable<TEntity>, IAsyncEnumerable<TEntity>
    where TEntity : class
{
    private readonly List<TEntity> _items;
    private readonly IQueryable<TEntity> _queryable;

    public FakeDbSet(params TEntity[] items)
    {
        _items = [.. items];
        _queryable = _items.AsQueryable();
    }

    public List<TEntity> Added { get; } = [];

    public List<TEntity> Removed { get; } = [];

    public IReadOnlyList<TEntity> Items => _items;

    public override IEntityType EntityType =>
        throw new NotSupportedException("The in-memory test set has no EF Core model.");

    Type IQueryable.ElementType => _queryable.ElementType;

    Expression IQueryable.Expression => _queryable.Expression;

    IQueryProvider IQueryable.Provider => new FakeAsyncQueryProvider<TEntity>(_queryable.Provider);

    public override EntityEntry<TEntity> Add(TEntity entity)
    {
        _items.Add(entity);
        Added.Add(entity);
        return null!;
    }

    public override EntityEntry<TEntity> Remove(TEntity entity)
    {
        _items.Remove(entity);
        Removed.Add(entity);
        return null!;
    }

    IEnumerator<TEntity> IEnumerable<TEntity>.GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

    IAsyncEnumerator<TEntity> IAsyncEnumerable<TEntity>.GetAsyncEnumerator(
        CancellationToken cancellationToken) =>
        new FakeAsyncEnumerator<TEntity>(_items.GetEnumerator());
}

internal sealed class FakeAsyncQueryProvider<TEntity> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    public FakeAsyncQueryProvider(IQueryProvider inner)
    {
        _inner = inner;
    }

    public IQueryable CreateQuery(Expression expression) =>
        new FakeAsyncQueryable<TEntity>(_inner.CreateQuery<TEntity>(expression));

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression) =>
        new FakeAsyncQueryable<TElement>(_inner.CreateQuery<TElement>(expression));

    public object? Execute(Expression expression) => _inner.Execute(expression);

    public TResult Execute<TResult>(Expression expression) => _inner.Execute<TResult>(expression);

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
    {
        var resultType = typeof(TResult).GetGenericArguments()[0];
        var executed = typeof(IQueryProvider)
            .GetMethods()
            .Single(method => method.Name == nameof(IQueryProvider.Execute) && method.IsGenericMethod)
            .MakeGenericMethod(resultType)
            .Invoke(_inner, [expression]);

        return (TResult)typeof(Task)
            .GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(resultType)
            .Invoke(null, [executed])!;
    }
}

internal sealed class FakeAsyncQueryable<TEntity>
    : IOrderedQueryable<TEntity>, IAsyncEnumerable<TEntity>
{
    private readonly IQueryable<TEntity> _inner;

    public FakeAsyncQueryable(IQueryable<TEntity> inner)
    {
        _inner = inner;
    }

    public Type ElementType => _inner.ElementType;

    public Expression Expression => _inner.Expression;

    public IQueryProvider Provider => new FakeAsyncQueryProvider<TEntity>(_inner.Provider);

    public IEnumerator<TEntity> GetEnumerator() => _inner.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _inner.GetEnumerator();

    public IAsyncEnumerator<TEntity> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
        new FakeAsyncEnumerator<TEntity>(_inner.GetEnumerator());
}

internal sealed class FakeAsyncEnumerator<TEntity> : IAsyncEnumerator<TEntity>
{
    private readonly IEnumerator<TEntity> _inner;

    public FakeAsyncEnumerator(IEnumerator<TEntity> inner)
    {
        _inner = inner;
    }

    public TEntity Current => _inner.Current;

    public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(_inner.MoveNext());

    public ValueTask DisposeAsync()
    {
        _inner.Dispose();
        return ValueTask.CompletedTask;
    }
}
