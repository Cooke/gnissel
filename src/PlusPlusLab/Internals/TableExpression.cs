using System.Linq.Expressions;
using PlusPlusLab.Querying;

namespace PlusPlusLab.Internals;

internal class TableExpression(TableSource tableSource) : Expression
{
    public TableSource TableSource { get; } = tableSource;

    public override ExpressionType NodeType => ExpressionType.Extension;

    public override Type Type => TableSource.Table.Type;
}