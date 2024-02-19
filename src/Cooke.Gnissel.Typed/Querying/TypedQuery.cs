using System.Linq.Expressions;
using Cooke.Gnissel.Queries;
using Cooke.Gnissel.Utils;

namespace Cooke.Gnissel.Typed.Querying;

public class TypedQuery<T>(DbOptionsTyped options, ExpressionQuery expressionQuery) : IToQuery<T>
{
    public TypedQuery<T> Where(Expression<Func<T, bool>> predicate) =>
        new(options, expressionQuery.Where(predicate));

    public Query<T> ToQuery() =>
        new(
            options.DbAdapter.RenderSql(options.SqlGenerator.Generate(expressionQuery)),
            options.ObjectReaderProvider.GetReaderFunc<T>(),
            options.DbConnector
        );

    public Query<TSelect> Select<TSelect>(Expression<Func<T, TSelect>> selector) =>
        expressionQuery.Select(selector).ToQuery<TSelect>(options);
}
