#region

using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using Cooke.Gnissel.CommandFactories;
using Cooke.Gnissel.Services;

#endregion

namespace Cooke.Gnissel.Statements;

public class TableQueryStatement<T> : IAsyncEnumerable<T>
{
    private readonly IDbAdapter _dbAdapter;
    private readonly string _fromTable;
    private readonly string? _condition;
    private readonly ImmutableArray<Column<T>> _columns;
    private readonly IRowReader _rowReader;
    private readonly IDbAccessFactory _dbAccessFactory;
    private readonly IQueryExecutor _queryExecutor;

    public TableQueryStatement(
        DbOptions options,
        string fromTable,
        string? condition,
        ImmutableArray<Column<T>> columns
    )
    {
        _dbAdapter = options.DbAdapter;
        _rowReader = options.RowReader;
        _dbAccessFactory = options.DbAccessFactory;
        _queryExecutor = options.QueryExecutor;
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
    )
    {
        return ExecuteAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);
    }

    public IAsyncEnumerable<T> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var sql = new Sql(100, 2);
        sql.AppendLiteral("SELECT * FROM ");
        sql.AppendLiteral(_dbAdapter.EscapeIdentifier(_fromTable));
        return _queryExecutor.Query(
            _dbAdapter.CompileSql(sql),
            _rowReader.Read<T>,
            _dbAccessFactory,
            cancellationToken
        );
    }

    public ImmutableArray<Column<T>> Columns
    {
        get => _columns;
        init => _columns = value;
    }
}
