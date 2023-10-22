using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Cooke.Gnissel;

public record QueryStatement<T>(
    DbContext DbContext,
    string? Condition,
    ImmutableArray<IColumn<T>> Columns
) : IAsyncEnumerable<T>
{
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
        sql.AppendLiteral("SELECT * FROM users");
        return DbContext.Query<T>(sql, cancellationToken);
    }
}
