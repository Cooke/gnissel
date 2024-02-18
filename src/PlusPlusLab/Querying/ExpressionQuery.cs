using System.Linq.Expressions;

namespace PlusPlusLab.Querying;

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
}

public record Join(TableSource TableSource, Expression? Condition);
