#region

using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using Cooke.Gnissel.Services;
using Cooke.Gnissel.Services.Implementations;
using Cooke.Gnissel.Utils;

#endregion

namespace Cooke.Gnissel.Statements;

public class TableQueryStatement<T> : IAsyncEnumerable<T>
{
    private readonly IDbAdapter _dbAdapter;
    private readonly string _fromTable;
    private readonly string? _condition;
    private readonly ImmutableArray<Column<T>> _columns;
    private readonly IDbConnector _dbConnector;
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

    [Pure]
    public TableQueryStatement<T> Where(Expression<Func<T, bool>> predicate)
    {
        // string condition = "";
        // switch (predicate.Body)
        // {
        //     case BinaryExpression exp:
        //     {
        //         Column<T> leftColumn;
        //         if (exp.Left is MemberExpression memberExp)
        //         {
        //             leftColumn = Columns.First(x => x.Member == memberExp.Member);
        //         }
        //
        //         object right;
        //         if (exp.Right is ConstantExpression constExp)
        //         {
        //             right = constExp.Value;
        //         }
        //
        //         condition = $"{leftColumn.Name} = {right}";
        //     }
        // }
        throw new NotImplementedException();
    }

    public TableQueryStatement<TOut> GroupJoin<TRight, TOut>(
        TableQueryStatement<TRight> right,
        Func<T, TRight, bool> func,
        Func<T, IEnumerable<TRight>, TOut> selector
    )
    {
        throw new NotImplementedException();
    }

    [Pure]
    public TableQueryStatement<TOut> Select<TOut>(Expression<Func<T, TOut>> selector)
    {
        throw new NotImplementedException();
    }

    public TableQueryStatement<TOut> Join<TRight, TOut>(
        Table<TRight> right,
        Expression<Func<T, TRight, bool>> predicate,
        Expression<Func<T, TRight, TOut>> selector
    )
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerator<T> GetAsyncEnumerator(
        CancellationToken cancellationToken = new CancellationToken()
    ) =>
        ExecuteAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);

    public IAsyncEnumerable<T> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var sql = new Sql(100, 2);
        sql.AppendLiteral("SELECT * FROM ");
        sql.AppendLiteral(_dbAdapter.EscapeIdentifier(_fromTable));
        var objectReader = _objectReaderProvider.Get<T>();
        return new QueryStatement<T>(
            _dbAdapter.RenderSql(sql),
            (reader, ct) => reader.ReadRows(objectReader, ct),
            _dbConnector
        );
    }

    public ImmutableArray<Column<T>> Columns
    {
        get => _columns;
        init => _columns = value;
    }
}
