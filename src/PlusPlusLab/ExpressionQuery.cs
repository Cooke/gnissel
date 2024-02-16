using System.Linq.Expressions;

namespace PlusPlusLab;

public record TableSource(ITable Table, string? Alias = null)
{
    public string AliasOrName => Alias ?? Table.Name;
}

public record ExpressionQuery(
    TableSource TableSource,
    Expression? Selector,
    IReadOnlyCollection<TableSource> Joins,
    IReadOnlyCollection<Expression> Conditions,
    int? Limit = null
);
