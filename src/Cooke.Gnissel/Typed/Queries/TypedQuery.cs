using System.Linq.Expressions;
using Cooke.Gnissel.Queries;

namespace Cooke.Gnissel.Typed.Queries;

public class TypedQuery<T>(ExpressionQuery expressionQuery) : IToQuery<T>
{
    public TypedQuery<T> Where(Expression<Func<T, bool>> predicate) =>
        new(expressionQuery.Where(predicate));

    public Query<T> ToQuery() => expressionQuery.ToQuery<T>();

    public SelectQuery<TSelect> Select<TSelect>(Expression<Func<T, TSelect>> selector) =>
        new(expressionQuery.Select(selector));
}
