using System.Linq.Expressions;
using Cooke.Gnissel.Queries;

namespace Cooke.Gnissel.Typed.Querying;

public class OrderByQuery<T>(DbOptionsTyped options, ExpressionQuery expressionQuery) : IToQuery<T>
{
    public Query<T> ToQuery() => expressionQuery.CreateQuery<T>(options);

    public OrderByQuery<T> ThenBy<TProp>(Expression<Func<T, TProp>> propSelector) =>
        new(options, expressionQuery.OrderBy(propSelector));

    public OrderByQuery<T> ThenByDesc<TProp>(Expression<Func<T, TProp>> propSelector) =>
        new(options, expressionQuery.OrderByDesc(propSelector));
}
