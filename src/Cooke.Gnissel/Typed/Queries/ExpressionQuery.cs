using System.Data.Common;
using System.Linq.Expressions;
using Cooke.Gnissel.Internals;
using Cooke.Gnissel.Queries;
using Cooke.Gnissel.Typed.Internals;

namespace Cooke.Gnissel.Typed.Queries;

public record TableSource(ITable Table, string? Alias = null)
{
    public string AliasOrName => Alias ?? Table.Name;
}

public record Join(JoinType Type, TableSource TableSource, Expression? Condition);

public enum JoinType
{
    Default,
    Left,
    Right,
    Full,
    Cross
}

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

    public ExpressionQuery Where(LambdaExpression predicate) =>
        this with
        {
            Conditions = [.. Conditions, Transform(predicate)]
        };

    public ExpressionQuery Select(LambdaExpression selector) =>
        this with
        {
            Selector = Transform(selector)
        };

    public ExpressionQuery Join(ITable joinTable, LambdaExpression predicate) =>
        Join(JoinType.Default, joinTable, predicate);

    public ExpressionQuery LeftJoin(ITable joinTable, LambdaExpression predicate) =>
        Join(JoinType.Left, joinTable, predicate);

    public ExpressionQuery RightJoin(ITable joinTable, LambdaExpression predicate) =>
        Join(JoinType.Right, joinTable, predicate);

    public ExpressionQuery FullJoin(ITable joinTable, LambdaExpression predicate) =>
        Join(JoinType.Full, joinTable, predicate);

    public ExpressionQuery CrossJoin(ITable joinTable) => Join(JoinType.Cross, joinTable, null);

    private ExpressionQuery Join(JoinType type, ITable joinTable, LambdaExpression? predicate)
    {
        var sameTableCount = Sources.Count(x => x.Table.Equals(joinTable));
        var joinAlias = sameTableCount > 0 ? joinTable.Name + "j" + sameTableCount : null;
        var joinSource = new TableSource(joinTable, joinAlias);
        return this with
        {
            Joins =
            [
                .. Joins,
                new Join(
                    type,
                    joinSource,
                    predicate?.Let(p =>
                        ParameterExpressionReplacer.Replace(
                            p.Body,
                            Sources
                                .Concat([joinSource])
                                .Select(
                                    (source, index) =>
                                        (
                                            predicate.Parameters[index],
                                            (Expression)new TableExpression(source)
                                        )
                                )
                                .ToArray()
                        )
                    )
                )
            ]
        };
    }

    public ExpressionQuery OrderBy(LambdaExpression propSelector) =>
        this with
        {
            OrderBys = [.. this.OrderBys, new(Transform(propSelector), false)]
        };

    public ExpressionQuery OrderByDesc(LambdaExpression propSelector) =>
        this with
        {
            OrderBys = [.. this.OrderBys, new(Transform(propSelector), true)]
        };

    public ExpressionQuery GroupBy(LambdaExpression propSelector) =>
        this with
        {
            Groupings = [.. this.Groupings, Transform(propSelector)]
        };

    public Query<T> ToQuery<T>() =>
        new(
            Options.RenderSql(Options.DbAdapter.TypedSqlGenerator.Generate(this, Options)),
            (reader, cancellationToken) =>
                reader.ReadRows(Options.GetReader<T>(), cancellationToken),
            Options.DbConnector
        );

    public SingleQuery<T> First<T>() => new((this with { Limit = 1 }).ToQuery<T>());

    public SingleOrDefaultQuery<T> FirstOrDefault<T>() =>
        new((this with { Limit = 1 }).ToQuery<T>());

    public SingleQuery<T> First<T>(LambdaExpression predicate) =>
        new(
            (
                this with
                {
                    Limit = 1,
                    Conditions = [.. this.Conditions, Transform(predicate)]
                }
            ).ToQuery<T>()
        );

    public SingleOrDefaultQuery<T> FirstOrDefault<T>(LambdaExpression predicate) =>
        new(
            (
                this with
                {
                    Limit = 1,
                    Conditions = [.. this.Conditions, Transform(predicate)]
                }
            ).ToQuery<T>()
        );

    private Expression Transform(LambdaExpression predicate) =>
        ParameterExpressionReplacer.Replace(
            predicate.Body,
            Sources
                .Take(predicate.Parameters.Count)
                .Select(
                    (source, index) =>
                        (predicate.Parameters[index], (Expression)new TableExpression(source))
                )
                .ToArray()
        );
}
