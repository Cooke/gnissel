using System.Linq.Expressions;
using Cooke.Gnissel.PlusPlus.Utils;
using Cooke.Gnissel.Queries;
using Cooke.Gnissel.Utils;

namespace PlusPlusLab;

public class TypedQuery<T>(DbOptionsPlus options, ExpressionQuery expressionQuery) : IToAsyncEnumerable<T>
{
    public TypedQuery<T> Where(Expression<Func<T, bool>> predicate)
    {
        var condition = ParameterExpressionReplacer.Replace(predicate.Body, [
            (predicate.Parameters.Single(), new TableExpression(expressionQuery.TableSource))])
        ;
        
        var newExp = expressionQuery with
        {
            Conditions = [..expressionQuery.Conditions, condition]
        };

        return new(options, newExp);
    }
    
    public IAsyncEnumerable<T> ToAsyncEnumerable() =>
        new Query<T>(
            options.DbAdapter.RenderSql(options.SqlGenerator.Generate(expressionQuery)),
            options.ObjectReaderProvider.GetReaderFunc<T>(),
            options.DbConnector
        );

    public TypedQuery<TSelect> Select<TSelect>(Expression<Func<T, TSelect>> selector) =>
        new(options, expressionQuery with
        {
            Selector = ParameterExpressionReplacer.Replace(selector.Body, [
                (selector.Parameters.Single(), new TableExpression(expressionQuery.TableSource))
            ])
        });
}
