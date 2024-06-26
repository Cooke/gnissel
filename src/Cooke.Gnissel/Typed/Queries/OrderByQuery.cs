﻿using System.Linq.Expressions;
using Cooke.Gnissel.Queries;

namespace Cooke.Gnissel.Typed.Queries;

public class OrderByQuery<T>(ExpressionQuery expressionQuery) : IQuery<T>
{
    private Query<T>? _query;
    private Query<T> LazyQuery => _query ??= expressionQuery.ToQuery<T>();

    public RenderedSql RenderedSql => LazyQuery.RenderedSql;

    public IAsyncEnumerable<T> ExecuteAsync(CancellationToken cancellationToken = default) =>
        LazyQuery.ExecuteAsync(cancellationToken);

    public OrderByQuery<T> ThenBy<TProp>(Expression<Func<T, TProp>> propSelector) =>
        new(expressionQuery.OrderBy(propSelector));

    public OrderByQuery<T> ThenByDesc<TProp>(Expression<Func<T, TProp>> propSelector) =>
        new(expressionQuery.OrderByDesc(propSelector));

    public TypedQuery<T> Limit(int limit) => new(expressionQuery with { Limit = limit });
}
