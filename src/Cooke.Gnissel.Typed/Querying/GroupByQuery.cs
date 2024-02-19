using System.Linq.Expressions;
using Cooke.Gnissel.Queries;

namespace Cooke.Gnissel.Typed.Querying;

public class GroupByQuery<T>(DbOptionsTyped options, ExpressionQuery expressionQuery) : IToQuery<T>
{
    public Query<T> ToQuery() => expressionQuery.ToQuery<T>(options);

    public GroupByQuery<T> ThenBy<TProp>(Expression<Func<T, TProp>> propSelector) =>
        new(options, expressionQuery.GroupBy(propSelector));

    public Query<TSelect> Select<TSelect>(Expression<Func<T, TSelect>> selector) =>
        expressionQuery.Select(selector).ToQuery<TSelect>(options);
}
