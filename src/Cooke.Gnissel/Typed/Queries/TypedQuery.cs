using System.Linq.Expressions;
using Cooke.Gnissel.Queries;

namespace Cooke.Gnissel.Typed.Queries;

public class TypedQuery<T>(ExpressionQuery expressionQuery) : IEnumerableQuery<T>
{
    private Query<T>? _query;
    private Query<T> LazyQuery => _query ??= expressionQuery.ToQuery<T>();

    public RenderedSql RenderedSql => LazyQuery.RenderedSql;

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new()) =>
        LazyQuery.GetAsyncEnumerator(cancellationToken);

    public TypedQuery<T> Where(Expression<Func<T, bool>> predicate) =>
        new(expressionQuery.Where(predicate));

    public SelectQuery<TSelect> Select<TSelect>(Expression<Func<T, TSelect>> selector) =>
        new(expressionQuery.Select(selector));

    public SingleQuery<T> First() => expressionQuery.First<T>();

    public SingleQuery<T> First(Expression<Func<T, bool>> predicate) =>
        expressionQuery.First<T>(predicate);

    public SingleOrDefaultQuery<T> FirstOrDefault() => expressionQuery.FirstOrDefault<T>();

    public SingleOrDefaultQuery<T> FirstOrDefault(Expression<Func<T, bool>> predicate) =>
        expressionQuery.FirstOrDefault<T>(predicate);

    public OrderByQuery<T> OrderBy<TProp>(Expression<Func<T, TProp>> propSelector) =>
        new(expressionQuery.OrderBy(propSelector));

    public OrderByQuery<T> OrderByDesc<TProp>(Expression<Func<T, TProp>> propSelector) =>
        new(expressionQuery.OrderByDesc(propSelector));

    public GroupByQuery<T> GroupBy<TProp>(Expression<Func<T, TProp>> propSelector) =>
        new(expressionQuery.GroupBy(propSelector));

    public TypedQuery<T> Limit(int limit) => new(expressionQuery with { Limit = limit });
}
