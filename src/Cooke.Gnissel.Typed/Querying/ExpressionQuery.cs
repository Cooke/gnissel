using System.Linq.Expressions;
using Cooke.Gnissel.Queries;
using Cooke.Gnissel.Typed.Internals;
using Cooke.Gnissel.Utils;

namespace Cooke.Gnissel.Typed.Querying;

public record TableSource(ITable Table, string? Alias = null)
{
    public string AliasOrName => Alias ?? Table.Name;
}

public record Join(TableSource TableSource, Expression? Condition);

public record By(Expression Expression, bool Descending);

public record ExpressionQuery(
    TableSource TableSource,
    Expression? Selector,
    IReadOnlyList<Join> Joins,
    IReadOnlyList<Expression> Conditions,
    IReadOnlyList<By> Order,
    int? Limit = null
)
{
    private IEnumerable<TableSource> Sources =>
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

    public ExpressionQuery WithOrderBy(LambdaExpression propSelector) =>
        this with
        {
            Order = [..this.Order, new (ReplaceParametersWithSources(propSelector), false)]
        };
    
    public ExpressionQuery WithOrderByDesc(LambdaExpression propSelector) =>
        this with
        {
            Order = [..this.Order, new (ReplaceParametersWithSources(propSelector), true)]
        };

    private Expression ReplaceParametersWithSources(LambdaExpression predicate) 
        => ParameterExpressionReplacer.Replace(
            predicate.Body, 
            Sources.Select((source, index) => (predicate.Parameters[index], (Expression)new TableExpression(source))).ToArray());

    public Query<T> CreateQuery<T>(DbOptionsTyped options) =>
        new(
            options.DbAdapter.RenderSql(options.SqlGenerator.Generate(this)),
            options.ObjectReaderProvider.GetReaderFunc<T>(),
            options.DbConnector
        );
}


