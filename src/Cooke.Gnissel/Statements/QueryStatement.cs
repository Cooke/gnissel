using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;

namespace Cooke.Gnissel;

public record QueryStatement<T>(string? Condition, ImmutableArray<IColumn<T>> Columns)
    : IAsyncEnumerable<T>
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
        return new QueryStatement<T>(null, Columns);
    }

    public QueryStatement<TOut> GroupJoin<TRight, TOut>(
        QueryStatement<TRight> right,
        Func<T, TRight, bool> func,
        Func<T, IEnumerable<TRight>, TOut> selector
    )
    {
        throw new NotImplementedException();
    }

    // [Pure]
    // public QueryStatement<TOut> Select<TOut>(Expression<Func<T, TOut>> selector)
    // {
    //     return new QueryStatement<TOut>(null, Columns);
    // }

    // public QueryStatement<TOut> Join<TRight, TOut>(
    //     Table<TRight> right,
    //     Expression<Func<T, TRight, bool>> predicate,
    //     Expression<Func<T, TRight, TOut>> selector
    // )
    // {
    //     throw new NotImplementedException();
    // }

    public IAsyncEnumerator<T> GetAsyncEnumerator(
        CancellationToken cancellationToken = new CancellationToken()
    )
    {
        throw new NotImplementedException();
    }
}
