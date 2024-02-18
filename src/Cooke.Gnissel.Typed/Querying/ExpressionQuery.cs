using System.Linq.Expressions;
using Cooke.Gnissel.Typed.Internals;

namespace Cooke.Gnissel.Typed.Querying;

public record TableSource(ITable Table, string? Alias = null)
{
    public string AliasOrName => Alias ?? Table.Name;
}

public record ExpressionQuery(
    TableSource TableSource,
    Expression? Selector,
    IReadOnlyList<Join> Joins,
    IReadOnlyList<Expression> Conditions,
    int? Limit = null
)
{
    public IEnumerable<TableSource> Sources =>
        new[] { TableSource }.Concat(Joins.Select(x => x.TableSource));

    public ExpressionQuery WithCondition(LambdaExpression predicate)
        => this with
        {
            Conditions =
            [
                ..Conditions,
                ReplaceParametersWithSources(predicate)
            ]
        };
    public ExpressionQuery WithSelect(LambdaExpression selector) =>
        this with
        {
            Selector = ReplaceParametersWithSources(selector)
        };
    
    public ExpressionQuery WithJoin(ITable joinTable, LambdaExpression predicate)
    {
        var sameTableCount = Sources.Count(x => x.Table.Equals(joinTable));
        var joinAlias = sameTableCount > 0 ? joinTable.Name + "j" + sameTableCount : null;
        var joinSource = new TableSource(joinTable, joinAlias);
        return this with
        {
            Joins =
            [
                ..Joins,
                new Join(
                    joinSource,
                    ParameterExpressionReplacer.Replace(
                        predicate.Body,
                        Sources.Concat([joinSource]).Select((source, index) => (predicate.Parameters[index], (Expression)new TableExpression(source))).ToArray())
                )
            ]
        };
    }
    
    private Expression ReplaceParametersWithSources(LambdaExpression predicate) 
        => ParameterExpressionReplacer.Replace(
            predicate.Body, 
            Sources.Select((source, index) => (predicate.Parameters[index], (Expression)new TableExpression(source))).ToArray());
}

public record Join(TableSource TableSource, Expression? Condition);
