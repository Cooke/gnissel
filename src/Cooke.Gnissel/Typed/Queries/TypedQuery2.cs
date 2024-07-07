using System.Linq.Expressions;
using Cooke.Gnissel.Queries;

namespace Cooke.Gnissel.Typed.Queries;

public class TypedQuery<T1, T2>(ExpressionQuery expressionQuery) : IQuery<(T1, T2)>
{
    private Query<(T1, T2)>? _query;
    private Query<(T1, T2)> LazyQuery => _query ??= expressionQuery.ToQuery<(T1, T2)>();

    public RenderedSql RenderedSql => LazyQuery.RenderedSql;

    public IAsyncEnumerable<(T1, T2)> ExecuteAsync(CancellationToken cancellationToken = default) =>
        LazyQuery.ExecuteAsync(cancellationToken);

    public TypedQuery<T1, T2> Where(Expression<Func<T1, bool>> predicate) =>
        new(expressionQuery.Where(predicate));

    public TypedQuery<T1, T2> Where(Expression<Func<T1, T2, bool>> predicate) =>
        new(expressionQuery.Where(predicate));

    public TypedQuery<T1, T2, T3> Join<T3>(
        Table<T3> joinTable,
        Expression<Func<T1, T2, T3, bool>> predicate
    ) => new(expressionQuery.Join(joinTable, predicate));

    public TypedQuery<T1, T2, T3?> LeftJoin<T3>(
        Table<T3> joinTable,
        Expression<Func<T1, T2, T3, bool>> predicate
    ) => new(expressionQuery.LeftJoin(joinTable, predicate));

    public TypedQuery<T1?, T2?, T3> RightJoin<T3>(
        Table<T3> joinTable,
        Expression<Func<T1, T2, T3, bool>> predicate
    ) => new(expressionQuery.RightJoin(joinTable, predicate));

    public TypedQuery<T1?, T2?, T3?> FullJoin<T3>(
        Table<T3> joinTable,
        Expression<Func<T1, T2, T3, bool>> predicate
    ) => new(expressionQuery.FullJoin(joinTable, predicate));

    public TypedQuery<T1, T2, T3> CrossJoin<T3>(Table<T3> joinTable) =>
        new(expressionQuery.CrossJoin(joinTable));

    public SingleQuery<(T1, T2)> First() => expressionQuery.First<(T1, T2)>();

    public SingleQuery<(T1, T2)> First(Expression<Func<T1, bool>> predicate) =>
        expressionQuery.First<(T1, T2)>(predicate);

    public SingleQuery<(T1, T2)> First(Expression<Func<T1, T2, bool>> predicate) =>
        expressionQuery.First<(T1, T2)>(predicate);

    public SingleOrDefaultQuery<(T1, T2)> FirstOrDefault() =>
        expressionQuery.FirstOrDefault<(T1, T2)>();

    public SingleOrDefaultQuery<(T1, T2)> FirstOrDefault(Expression<Func<T1, bool>> predicate) =>
        expressionQuery.FirstOrDefault<(T1, T2)>(predicate);

    public SingleOrDefaultQuery<(T1, T2)> FirstOrDefault(
        Expression<Func<T1, T2, bool>> predicate
    ) => expressionQuery.FirstOrDefault<(T1, T2)>(predicate);

    public TypedQuery<T1, T2> Limit(int limit) => new(expressionQuery with { Limit = limit });
}
