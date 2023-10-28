using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using Cooke.Gnissel.CommandFactories;
using Cooke.Gnissel.Services;

namespace Cooke.Gnissel.Statements;

public class QueryStatement<T> : IAsyncEnumerable<T>
{
    private readonly IDbAdapter _dbAdapter;
    private readonly string _fromTable;
    private readonly string? _condition;
    private readonly ImmutableArray<Column<T>> _columns;
    private readonly IObjectMapper _objectMapper;
    private readonly ICommandFactory _commandFactory;
    private readonly IQueryExecutor _queryExecutor;

    public QueryStatement(
        DbOptions options,
        string fromTable,
        string? condition,
        ImmutableArray<Column<T>> columns
    )
    {
        _dbAdapter = options.DbAdapter;
        _objectMapper = options.ObjectMapper;
        _commandFactory = options.CommandFactory;
        _queryExecutor = options.QueryExecutor;
        _fromTable = fromTable;
        _condition = condition;
        Columns = columns;
    }

    [Pure]
    public QueryStatement<T> Where(Expression<Func<T, bool>> predicate)
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

    public QueryStatement<TOut> GroupJoin<TRight, TOut>(
        QueryStatement<TRight> right,
        Func<T, TRight, bool> func,
        Func<T, IEnumerable<TRight>, TOut> selector
    )
    {
        throw new NotImplementedException();
    }

    [Pure]
    public QueryStatement<TOut> Select<TOut>(Expression<Func<T, TOut>> selector)
    {
        throw new NotImplementedException();
    }

    public QueryStatement<TOut> Join<TRight, TOut>(
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
        var sql = new FormattedSql(100, 2);
        sql.AppendLiteral("SELECT * FROM ");
        sql.AppendLiteral(_dbAdapter.EscapeIdentifier(_fromTable));
        return _queryExecutor.Execute(
            sql,
            _objectMapper.Map<T>,
            _commandFactory,
            _dbAdapter,
            cancellationToken
        );
    }

    public ImmutableArray<Column<T>> Columns
    {
        get => _columns;
        init => _columns = value;
    }
}
