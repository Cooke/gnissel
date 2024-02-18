using System.Linq.Expressions;
using Cooke.Gnissel.Queries;
using Cooke.Gnissel.Utils;
using PlusPlusLab.Utils;

namespace PlusPlusLab.Querying;

public class TypedQuery<T1, T2, T3>(DbOptionsPlus options, ExpressionQuery expressionQuery) : IToAsyncEnumerable<(T1,T2,T3)>
{
    public TypedQuery<T1, T2, T3> Where(Expression<Func<T1, T2, T3, bool>> predicate)
    {
        var condition = ParameterExpressionReplacer.Replace(predicate.Body, [
            (predicate.Parameters[0], new TableExpression(expressionQuery.TableSource)),
            (predicate.Parameters[1], new TableExpression(expressionQuery.Joins[0].TableSource))])
        ;
        
        var newExp = expressionQuery with
        {
            Conditions = [..expressionQuery.Conditions, condition]
        };

        return new(options, newExp);
    }
    
    public IAsyncEnumerable<(T1, T2, T3)> ToAsyncEnumerable() =>
        new Query<(T1, T2, T3)>(
            options.DbAdapter.RenderSql(options.SqlGenerator.Generate(expressionQuery)),
            options.ObjectReaderProvider.GetReaderFunc<(T1, T2, T3)>(),
            options.DbConnector
        );
}
