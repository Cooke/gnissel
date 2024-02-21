using System.Linq.Expressions;
using Cooke.Gnissel.Queries;

namespace Cooke.Gnissel.Typed.Queries;

public class OrderByQuery<T>(ExpressionQuery expressionQuery) : IToQuery<T>
{
    public Query<T> ToQuery() => expressionQuery.ToQuery<T>();

    public OrderByQuery<T> ThenBy<TProp>(Expression<Func<T, TProp>> propSelector) =>
        new(expressionQuery.OrderBy(propSelector));

    public OrderByQuery<T> ThenByDesc<TProp>(Expression<Func<T, TProp>> propSelector) =>
        new(expressionQuery.OrderByDesc(propSelector));
}
