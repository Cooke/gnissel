﻿using System.Linq.Expressions;
using Cooke.Gnissel.Queries;

namespace Cooke.Gnissel.Typed.Queries;

public class GroupByQuery<T1, T2>(ExpressionQuery expressionQuery) : IQuery<(T1, T2)>
{
    private Query<(T1, T2)>? _query;
    private Query<(T1, T2)> LazyQuery => _query ??= expressionQuery.ToQuery<(T1, T2)>();

    public RenderedSql RenderedSql => LazyQuery.RenderedSql;

    public IAsyncEnumerable<(T1, T2)> ExecuteAsync(CancellationToken cancellationToken = default) =>
        LazyQuery.ExecuteAsync(cancellationToken);

    public GroupByQuery<T1, T2> ThenBy<TProp>(Expression<Func<T1, T2, TProp>> propSelector) =>
        new(expressionQuery.GroupBy(propSelector));

    public SelectQuery<TSelect> Select<TSelect>(Expression<Func<T1, T2, TSelect>> selector) =>
        new(expressionQuery.Select(selector));
}
