using System.Linq.Expressions;
using Cooke.Gnissel.Queries;

namespace Cooke.Gnissel.Typed.Queries;

public class TypedQuery<T1, T2, T3, T4>(ExpressionQuery expressionQuery) : IQuery<(T1, T2, T3, T4)>
{
    private Query<(T1, T2, T3, T4)>? _query;
    private Query<(T1, T2, T3, T4)> LazyQuery =>
        _query ??= expressionQuery.ToQuery<(T1, T2, T3, T4)>();

    public RenderedSql RenderedSql => LazyQuery.RenderedSql;

    public IAsyncEnumerable<(T1, T2, T3, T4)> ExecuteAsync(
        CancellationToken cancellationToken = default
    ) => LazyQuery.ExecuteAsync(cancellationToken);

    public TypedQuery<T1, T2, T3, T4> Where(Expression<Func<T1, bool>> predicate) =>
        new(expressionQuery.Where(predicate));

    public TypedQuery<T1, T2, T3, T4> Where(Expression<Func<T1, T2, bool>> predicate) =>
        new(expressionQuery.Where(predicate));

    public TypedQuery<T1, T2, T3, T4> Where(Expression<Func<T1, T2, T3, T4, bool>> predicate) =>
        new(expressionQuery.Where(predicate));

    public OrderByQuery<T1, T2, T3, T4> OrderBy<TProp>(
        Expression<Func<T1, T2, T3, T4, TProp>> propSelector
    ) => new(expressionQuery.OrderBy(propSelector));

    public OrderByQuery<T1, T2, T3, T4> OrderByDesc<TProp>(
        Expression<Func<T1, TProp>> propSelector
    ) => new(expressionQuery.OrderByDesc(propSelector));

    public OrderByQuery<T1, T2, T3, T4> OrderByDesc<TProp>(
        Expression<Func<T1, T2, T3, T4, TProp>> propSelector
    ) => new(expressionQuery.OrderByDesc(propSelector));

    public GroupByQuery<T1, T2, T3, T4> GroupBy<TProp>(
        Expression<Func<T1, T2, T3, T4, TProp>> propSelector
    ) => new(expressionQuery.GroupBy(propSelector));

    public SingleQuery<(T1, T2, T3, T4)> First() => expressionQuery.First<(T1, T2, T3, T4)>();

    public SingleQuery<(T1, T2, T3, T4)> First(Expression<Func<T1, T2, bool>> predicate) =>
        expressionQuery.First<(T1, T2, T3, T4)>(predicate);

    public SingleOrDefaultQuery<(T1, T2, T3, T4)> FirstOrDefault() =>
        expressionQuery.FirstOrDefault<(T1, T2, T3, T4)>();

    public SingleOrDefaultQuery<(T1, T2, T3, T4)> FirstOrDefault(
        Expression<Func<T1, bool>> predicate
    ) => expressionQuery.FirstOrDefault<(T1, T2, T3, T4)>(predicate);

    public SingleOrDefaultQuery<(T1, T2, T3, T4)> FirstOrDefault(
        Expression<Func<T1, T2, bool>> predicate
    ) => expressionQuery.FirstOrDefault<(T1, T2, T3, T4)>(predicate);

    public SingleOrDefaultQuery<(T1, T2, T3, T4)> FirstOrDefault(
        Expression<Func<T1, T2, T3, T4, bool>> predicate
    ) => expressionQuery.FirstOrDefault<(T1, T2, T3, T4)>(predicate);

    public TypedQuery<T1, T2, T3, T4> Limit(int limit) =>
        new(expressionQuery with { Limit = limit });
}
