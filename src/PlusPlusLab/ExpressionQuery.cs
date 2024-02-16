using System.Linq.Expressions;

namespace PlusPlusLab;

public record TableSource(ITable Table, string? Alias = null)
{
    public string AliasOrName => Alias ?? Table.Name;
}

public record ExpressionQuery(
    TableSource Table,
    Expression? Selector,
    IReadOnlyCollection<TableSource> Joins
);
