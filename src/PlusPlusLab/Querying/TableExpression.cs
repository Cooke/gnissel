using System.Linq.Expressions;

namespace PlusPlusLab.Querying;

public class TableExpression(TableSource tableSource) : Expression
{
    public TableSource TableSource { get; } = tableSource;

    public override ExpressionType NodeType => ExpressionType.Extension;

    public override Type Type => TableSource.Table.Type;
}