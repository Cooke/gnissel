using System.Linq.Expressions;
using Cooke.Gnissel.Queries;

namespace Cooke.Gnissel.Typed.Queries;

public class TypedQuery<T1, T2>(ExpressionQuery expressionQuery) : IToQuery<(T1, T2)>
{
    public TypedQuery<T1, T2> Where(Expression<Func<T1, T2, bool>> predicate) =>
        new(expressionQuery.Where(predicate));

    public Query<(T1, T2)> ToQuery() => expressionQuery.ToQuery<(T1, T2)>();

    public TypedQuery<T1, T2, T3> Join<T3>(
        Table<T3> joinTable,
        Expression<Func<T1, T2, T3, bool>> predicate
    ) => new(expressionQuery.Join(joinTable, predicate));

    public FirstQuery<(T1, T2)> First() => new(expressionQuery);

    public FirstQuery<(T1, T2)> First(Expression<Func<T1, T2, bool>> predicate) =>
        new(expressionQuery.Where(predicate));
}