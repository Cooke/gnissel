using System.Linq.Expressions;
using Cooke.Gnissel.Queries;
using Cooke.Gnissel.Utils;

namespace Cooke.Gnissel.Typed.Querying;

public class TypedQuery<T1, T2, T3>(ExpressionQuery expressionQuery) : IToQuery<(T1, T2, T3)>
{
    public TypedQuery<T1, T2, T3> Where(Expression<Func<T1, T2, T3, bool>> predicate) =>
        new(expressionQuery.Where(predicate));

    public Query<(T1, T2, T3)> ToQuery() => expressionQuery.ToQuery<(T1, T2, T3)>();
}
