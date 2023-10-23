using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Cooke.Gnissel;

public class QueryStatement<T> : IAsyncEnumerable<T>
{
    private readonly DbAdapter _dbAdapter;
    private readonly string _fromTable;
    private readonly DbContext _dbContext;
    private readonly string? _condition;
    private readonly ImmutableArray<IColumn<T>> _columns;

    public QueryStatement(
        DbAdapter dbAdapter,
        string fromTable,
        DbContext dbContext,
        string? condition,
        ImmutableArray<IColumn<T>> columns
    )
    {
        _dbAdapter = dbAdapter;
        _fromTable = fromTable;
        _dbContext = dbContext;
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
        //         IColumn<T> leftColumn;
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
        var sql = new ParameterizedSql(100, 2);
        sql.AppendLiteral("SELECT * FROM ");
        sql.AppendLiteral(_dbAdapter.EscapeIdentifier(_fromTable));
        return _dbContext.Query<T>(sql, cancellationToken);
    }

    public ImmutableArray<IColumn<T>> Columns
    {
        get => _columns;
        init => _columns = value;
    }
}
