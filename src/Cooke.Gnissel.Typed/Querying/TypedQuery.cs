using System.Linq.Expressions;
using Cooke.Gnissel.Queries;
using Cooke.Gnissel.Utils;

namespace Cooke.Gnissel.Typed.Querying;

public class TypedQuery<T>(DbOptionsTyped options, ExpressionQuery expressionQuery) : IToQuery<T>
{
    public TypedQuery<T> Where(Expression<Func<T, bool>> predicate) 
        => new TypedQuery<T>(options, expressionQuery.WithCondition(predicate));

    public Query<T> ToQuery() =>
        new Query<T>(
            options.DbAdapter.RenderSql(options.SqlGenerator.Generate(expressionQuery)),
            options.ObjectReaderProvider.GetReaderFunc<T>(),
            options.DbConnector
        );

    public TypedQuery<TSelect> Select<TSelect>(Expression<Func<T, TSelect>> selector) =>
        new(options, expressionQuery.WithSelect(selector));
}
