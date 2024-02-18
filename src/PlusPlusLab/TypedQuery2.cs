using System.Linq.Expressions;
using Cooke.Gnissel.PlusPlus.Utils;
using Cooke.Gnissel.Queries;
using Cooke.Gnissel.Utils;

namespace PlusPlusLab;

public class TypedQuery<T1, T2>(DbOptionsPlus options, ExpressionQuery expressionQuery) : IToAsyncEnumerable<(T1,T2)>
{
    public TypedQuery<T1, T2> Where(Expression<Func<T1, T2, bool>> predicate)
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
    
    public IAsyncEnumerable<(T1, T2)> ToAsyncEnumerable() =>
        new Query<(T1, T2)>(
            options.DbAdapter.RenderSql(options.SqlGenerator.Generate(expressionQuery)),
            options.ObjectReaderProvider.GetReaderFunc<(T1, T2)>(),
            options.DbConnector
        );

    public TypedQuery<T1, T2, T3> Join<T3>(Table<T3> joinTable, Expression<Func<T1, T2, T3, bool>> predicate)
    {
        var tableCount = expressionQuery.Sources.Count(x => x.Table.Equals(joinTable));
        var joinSource = new TableSource(joinTable, tableCount > 0 ? $"{joinTable.Name}_j{tableCount + 1}" : null);
        
        var joinCondition =ParameterExpressionReplacer.Replace(predicate.Body, [
            (predicate.Parameters[0], new TableExpression(expressionQuery.TableSource)),
            (predicate.Parameters[1], new TableExpression(expressionQuery.Joins[0].TableSource)),
            (predicate.Parameters[2], new TableExpression(joinSource))
        ]);

        var join = new Join(joinSource, joinCondition);

        return new(options, expressionQuery with
        {
            Joins = [..expressionQuery.Joins, join]
        });
    }

    public FirstQuery<(T1, T2)> First() => new(options, expressionQuery);
    
    public FirstQuery<(T1, T2)> First(Expression<Func<T1, T2, bool>> predicate) => new(options, 
        expressionQuery with{ Conditions = [..expressionQuery.Conditions, ParameterExpressionReplacer.Replace(predicate.Body, [
        (predicate.Parameters[0], new TableExpression(expressionQuery.TableSource)),
        (predicate.Parameters[1], new TableExpression(expressionQuery.Joins[0].TableSource))
    ])]});
}
