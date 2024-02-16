using System.Linq.Expressions;
using Cooke.Gnissel.PlusPlus.Utils;
using Cooke.Gnissel.Queries;
using Cooke.Gnissel.Utils;

namespace PlusPlusLab;

public class SelectQuery<TTable, TSelect> : IToAsyncEnumerable<TSelect>
{
    private readonly DbOptionsPlus _options;
    private readonly ExpressionQuery _expressionQuery;
    
    public SelectQuery(
        Table<TTable> table,
        DbOptionsPlus options,
        Expression<Func<TTable, TSelect>> selector
    )
    {
        _options = options;

        var tableSource = new TableSource(table);
        _expressionQuery = new ExpressionQuery(
            tableSource, 
                ParameterExpressionReplacer.Replace(selector.Body, [
                    (selector.Parameters.Single(), new TableExpression(tableSource))
                ])
            , []);
    }
    
    public IAsyncEnumerable<TSelect> ToAsyncEnumerable() => new Query<TSelect>(
        _options.DbAdapter.RenderSql(_options.SqlGenerator.Generate(_expressionQuery)),
        _options.ObjectReaderProvider.GetReaderFunc<TSelect>(),
        _options.DbConnector
    );
}