using System.Linq.Expressions;
using Cooke.Gnissel.Queries;

namespace Cooke.Gnissel.Typed.Queries;

public class TypedQuery<T1, T2, T3>(ExpressionQuery expressionQuery) : IQuery<(T1, T2, T3)>
{
    private Query<(T1, T2, T3)>? _query;
    private Query<(T1, T2, T3)> LazyQuery => _query ??= expressionQuery.ToQuery<(T1, T2, T3)>();

    public RenderedSql RenderedSql => LazyQuery.RenderedSql;

    public IAsyncEnumerable<(T1, T2, T3)> ExecuteAsync(
        CancellationToken cancellationToken = default
    ) => LazyQuery.ExecuteAsync(cancellationToken);

    public TypedQuery<T1, T2, T3> Where(Expression<Func<T1, bool>> predicate) =>
        new(expressionQuery.Where(predicate));

    public TypedQuery<T1, T2, T3> Where(Expression<Func<T1, T2, bool>> predicate) =>
        new(expressionQuery.Where(predicate));

    public TypedQuery<T1, T2, T3> Where(Expression<Func<T1, T2, T3, bool>> predicate) =>
        new(expressionQuery.Where(predicate));

    public TypedQuery<T1, T2, T3, T4> Join<T4>(
        Table<T4> joinTable,
        Expression<Func<T1, T2, T3, T4, bool>> predicate
    ) => new(expressionQuery.Join(joinTable, predicate));

    public TypedQuery<T1, T2, T3, T4?> LeftJoin<T4>(
        Table<T4> joinTable,
        Expression<Func<T1, T2, T3, T4, bool>> predicate
    ) => new(expressionQuery.LeftJoin(joinTable, predicate));

    public TypedQuery<T1?, T2?, T3?, T4> RightJoin<T4>(
        Table<T4> joinTable,
        Expression<Func<T1, T2, T3, T4, bool>> predicate
    ) => new(expressionQuery.RightJoin(joinTable, predicate));

    public TypedQuery<T1?, T2?, T3?, T4?> FullJoin<T4>(
        Table<T4> joinTable,
        Expression<Func<T1, T2, T3, T4, bool>> predicate
    ) => new(expressionQuery.FullJoin(joinTable, predicate));

    public TypedQuery<T1, T2, T3, T4> CrossJoin<T4>(Table<T4> joinTable) =>
        new(expressionQuery.CrossJoin(joinTable));

    public OrderByQuery<T1, T2, T3> OrderBy<TProp>(
        Expression<Func<T1, T2, T3, TProp>> propSelector
    ) => new(expressionQuery.OrderBy(propSelector));

    public OrderByQuery<T1, T2, T3> OrderByDesc<TProp>(
        Expression<Func<T1, T2, T3, TProp>> propSelector
    ) => new(expressionQuery.OrderByDesc(propSelector));

    public GroupByQuery<T1, T2, T3> GroupBy<TProp>(
        Expression<Func<T1, T2, T3, TProp>> propSelector
    ) => new(expressionQuery.GroupBy(propSelector));

    public SingleQuery<(T1, T2, T3)> First() => expressionQuery.First<(T1, T2, T3)>();

    public SingleQuery<(T1, T2, T3)> First(Expression<Func<T1, T2, bool>> predicate) =>
        expressionQuery.First<(T1, T2, T3)>(predicate);

    public SingleOrDefaultQuery<(T1, T2, T3)> FirstOrDefault() =>
        expressionQuery.FirstOrDefault<(T1, T2, T3)>();

    public SingleOrDefaultQuery<(T1, T2, T3)> FirstOrDefault(
        Expression<Func<T1, bool>> predicate
    ) => expressionQuery.FirstOrDefault<(T1, T2, T3)>(predicate);

    public SingleOrDefaultQuery<(T1, T2, T3)> FirstOrDefault(
        Expression<Func<T1, T2, bool>> predicate
    ) => expressionQuery.FirstOrDefault<(T1, T2, T3)>(predicate);

    public SingleOrDefaultQuery<(T1, T2, T3)> FirstOrDefault(
        Expression<Func<T1, T2, T3, bool>> predicate
    ) => expressionQuery.FirstOrDefault<(T1, T2, T3)>(predicate);

    public TypedQuery<T1, T2, T3> Limit(int limit) => new(expressionQuery with { Limit = limit });
}
