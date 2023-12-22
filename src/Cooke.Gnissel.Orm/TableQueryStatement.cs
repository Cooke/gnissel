#region

using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using Cooke.Gnissel.Services;
using Cooke.Gnissel.Utils;

#endregion

namespace Cooke.Gnissel.Statements;

public class TableQueryStatement<T> : IAsyncEnumerable<T>
{
    private readonly IDbAdapter _dbAdapter;
    private readonly IDbConnector _dbConnector;
    private readonly string _fromTable;
    private readonly IObjectReaderProvider _objectReaderProvider;
    private string? _condition;

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
    public TableQueryStatement<T> Where(Expression<Func<T, bool>> predicate)
    {
        var condition = "";
        switch (predicate.Body)
        {
            case BinaryExpression exp:
            {
                Column<T> leftColumn;
                if (exp.Left is MemberExpression memberExp)
                    leftColumn = Columns.First(x => x.Member == memberExp.Member);
                else
                    throw new NotSupportedException();

                object right;
                if (exp.Right is ConstantExpression constExp)
                    right = RenderConstant(constExp.Value);
                else
                    throw new NotSupportedException();

                condition = $"{leftColumn.Name} = {right}";
                break;
            }

            default:
                throw new NotSupportedException();
        }

        _condition = condition;
        return this;
    }

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