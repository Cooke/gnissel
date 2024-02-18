using System.Linq.Expressions;
using Cooke.Gnissel.Queries;
using Cooke.Gnissel.Utils;

namespace Cooke.Gnissel.Typed.Querying;

public class TypedQuery<T1, T2, T3>(DbOptionsTyped options, ExpressionQuery expressionQuery) : IToQuery<(T1,T2,T3)>
{
    public TypedQuery<T1, T2, T3> Where(Expression<Func<T1, T2, T3, bool>> predicate) => 
        new (options, expressionQuery.WithCondition(predicate));

    public Query<(T1, T2, T3)> ToQuery() =>
        new (
            options.DbAdapter.RenderSql(options.SqlGenerator.Generate(expressionQuery)),
            options.ObjectReaderProvider.GetReaderFunc<(T1, T2, T3)>(),
            options.DbConnector
        );
}
