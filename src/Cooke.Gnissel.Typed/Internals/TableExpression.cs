using System.Linq.Expressions;
using Cooke.Gnissel.Typed.Querying;

namespace Cooke.Gnissel.Typed.Internals;

public class TableExpression(TableSource tableSource) : Expression
{
    public TableSource TableSource { get; } = tableSource;

    public override ExpressionType NodeType => ExpressionType.Extension;

    public override Type Type => TableSource.Table.Type;
}