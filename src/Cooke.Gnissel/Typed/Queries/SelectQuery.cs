using Cooke.Gnissel.Queries;

namespace Cooke.Gnissel.Typed.Queries;

public class SelectQuery<T>(ExpressionQuery expressionQuery) : IQuery<T>
{
    private Query<T>? _query;
    private Query<T> LazyQuery => _query ??= expressionQuery.ToQuery<T>();

    public RenderedSql RenderedSql => LazyQuery.RenderedSql;

    public IAsyncEnumerable<T> ExecuteAsync(CancellationToken cancellationToken = default) =>
        LazyQuery.ExecuteAsync(cancellationToken);

    public SingleQuery<T> First() => expressionQuery.First<T>();

    public SingleOrDefaultQuery<T> FirstOrDefault() => expressionQuery.FirstOrDefault<T>();
}
