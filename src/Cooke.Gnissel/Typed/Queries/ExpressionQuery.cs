using System.Linq.Expressions;
using Cooke.Gnissel.Queries;
using Cooke.Gnissel.Typed.Internals;
using Cooke.Gnissel.Utils;

namespace Cooke.Gnissel.Typed.Queries;

public record TableSource(ITable Table, string? Alias = null)
{
    public string AliasOrName => Alias ?? Table.Name;
}

public record Join(TableSource TableSource, Expression? Condition);

public record OrderBy(Expression Expression, bool Descending);

public record ExpressionQuery(
    DbOptions Options,
    TableSource TableSource,
    Expression? Selector,
    IReadOnlyList<Join> Joins,
    IReadOnlyList<Expression> Conditions,
    IReadOnlyList<Expression> Groupings,
    IReadOnlyList<OrderBy> OrderBys,
    int? Limit = null
)
{
    private IEnumerable<TableSource> Sources =>
        new[] { TableSource }.Concat(Joins.Select(x => x.TableSource));

    public ExpressionQuery Where(LambdaExpression predicate)
        => this with
        {
            Conditions =
            [
                ..Conditions,
                Transform(predicate)
            ]
        };
    public ExpressionQuery Select(LambdaExpression selector) =>
        this with
        {
            Selector = Transform(selector)
        };
    
    public ExpressionQuery Join(ITable joinTable, LambdaExpression predicate)
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

    public ExpressionQuery OrderBy(LambdaExpression propSelector) =>
        this with
        {
            OrderBys = [..this.OrderBys, new (Transform(propSelector), false)]
        };
    
    public ExpressionQuery OrderByDesc(LambdaExpression propSelector) =>
        this with
        {
            OrderBys = [..this.OrderBys, new (Transform(propSelector), true)]
        };

    public ExpressionQuery GroupBy(LambdaExpression propSelector)
        => this with
        {
            Groupings = [..this.Groupings, Transform(propSelector)]
        };

    public Query<T> ToQuery<T>() =>
        new(
            Options.DbAdapter.RenderSql(Options.TypedSqlGenerator.Generate(this)),
            Options.ObjectReaderProvider.GetReaderFunc<T>(),
            Options.DbConnector
        );

    private Expression Transform(LambdaExpression predicate) 
        => ParameterExpressionReplacer.Replace(
            predicate.Body, 
            Sources.Select((source, index) => (predicate.Parameters[index], (Expression)new TableExpression(source))).ToArray());
}


