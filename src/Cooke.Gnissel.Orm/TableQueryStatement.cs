#region

using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using System.Reflection;
using Cooke.Gnissel.Services;
using Cooke.Gnissel.Utils;

#endregion

namespace Cooke.Gnissel.Statements;

public class TableQueryStatement<T> : IAsyncEnumerable<T>
{
    private readonly string? _condition;
    private readonly IDbAdapter _dbAdapter;
    private readonly IDbConnector _dbConnector;
    private readonly string _fromTable;
    private readonly IObjectReaderProvider _objectReaderProvider;

    public TableQueryStatement(
        DbOptions options,
        string fromTable,
        string? condition,
        ImmutableArray<Column<T>> columns
    )
    {
        _objectReaderProvider = options.ObjectReaderProvider;
        _dbAdapter = options.DbAdapter;
        _dbConnector = options.DbConnector;
        _fromTable = fromTable;
        _condition = condition;
        Columns = columns;
    }

    public ImmutableArray<Column<T>> Columns { get; init; }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new())
    {
        return ExecuteAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);
    }

    [Pure]
    public TableQueryStatement<T> Where(Expression<Func<T, bool>> predicate) =>
        new(
            new DbOptions(_dbAdapter, _objectReaderProvider, _dbConnector),
            _fromTable,
            RenderExpression(predicate.Body, predicate.Parameters[0]),
            Columns
        );

    private string RenderExpression(
        Expression expression,
        ParameterExpression parameterExpression
    ) =>
        expression switch
        {
            BinaryExpression binaryExpression
                => $"{RenderExpression(binaryExpression.Left, parameterExpression)} {RenderBinaryOperator(binaryExpression.NodeType)} {RenderExpression(binaryExpression.Right, parameterExpression)}",

            ConstantExpression constExp => RenderConstant(constExp.Value),

            MemberExpression memberExpression
                when memberExpression.Expression == parameterExpression
                => Columns.First(x => x.Member == memberExpression.Member).Name,

            MemberExpression
            {
                Expression: ConstantExpression constantExpression,
                Member: FieldInfo field
            }
                => RenderConstant(field.GetValue(constantExpression.Value)),

            _ => throw new NotSupportedException()
        };

    private string RenderBinaryOperator(ExpressionType expressionType) =>
        expressionType switch
        {
            ExpressionType.Equal => "=",
            ExpressionType.GreaterThan => ">",
            ExpressionType.GreaterThanOrEqual => ">=",
            ExpressionType.LessThan => "<",
            ExpressionType.LessThanOrEqual => "<=",
            ExpressionType.Multiply => "*",
            ExpressionType.NotEqual => "<>",
            ExpressionType.Or => "OR",
            ExpressionType.Subtract => "-",
            _ => throw new ArgumentOutOfRangeException(nameof(expressionType), expressionType, null)
        };

    private string RenderConstant(object? value)
    {
        return value switch
        {
            string => $"'{value}'",
            _ => value?.ToString() ?? "NULL"
        };
    }

    public IAsyncEnumerable<T> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var sql = new Sql(100, 2);
        sql.AppendLiteral("SELECT * FROM ");
        sql.AppendLiteral(_dbAdapter.EscapeIdentifier(_fromTable));
        if (_condition != null)
        {
            sql.AppendLiteral(" WHERE ");
            sql.AppendLiteral(_condition);
        }

        var objectReader = _objectReaderProvider.Get<T>();
        return new QueryStatement<T>(
            _dbAdapter.RenderSql(sql),
            (reader, ct) => reader.ReadRows(objectReader, ct),
            _dbConnector
        );
    }
}
