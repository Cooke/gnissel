using System.Linq.Expressions;
using Cooke.Gnissel.Queries;
using Cooke.Gnissel.Utils;
using PlusPlusLab.Utils;

namespace PlusPlusLab.Querying;

public class TypedQuery<T1, T2>(DbOptionsPlus options, ExpressionQuery expressionQuery) : IToAsyncEnumerable<(T1,T2)>
{
    public TypedQuery<T1, T2> Where(Expression<Func<T1, T2, bool>> predicate) 
        => new TypedQuery<T1, T2>(options, expressionQuery.WithCondition(predicate));

    public IAsyncEnumerable<(T1, T2)> ToAsyncEnumerable() =>
        new Query<(T1, T2)>(
            options.DbAdapter.RenderSql(options.SqlGenerator.Generate(expressionQuery)),
            options.ObjectReaderProvider.GetReaderFunc<(T1, T2)>(),
            options.DbConnector
        );

    public TypedQuery<T1, T2, T3> Join<T3>(Table<T3> joinTable, Expression<Func<T1, T2, T3, bool>> predicate) 
        => new TypedQuery<T1, T2, T3>(options, expressionQuery.WithJoin(joinTable, predicate));

    public FirstQuery<(T1, T2)> First() => new(options, expressionQuery);
    
    public FirstQuery<(T1, T2)> First(Expression<Func<T1, T2, bool>> predicate) 
        => new(options, expressionQuery.WithCondition(predicate));
}
