using System.Linq.Expressions;

namespace Cooke.Gnissel.PlusPlus.Utils;

internal static class PredicateTransformer
{
    public static Expression<Func<T, bool>> CreateTuplePredicate<T>(
        LambdaExpression parameterPredicate
    )
    {
        var tupleType = typeof(ValueTuple<,>).MakeGenericType(
            parameterPredicate.Parameters.Select(x => x.Type).ToArray()
        );
        var tupleParameter = Expression.Parameter(tupleType);
        var parameterTransformer = new ParameterTransformer(
            parameterPredicate.Parameters
                .Select(
                    (p, i) =>
                        (p, (Expression)Expression.PropertyOrField(tupleParameter, $"Item{i + 1}"))
                )
                .ToArray()
        );

        var tupleBody = parameterTransformer.Visit(parameterPredicate.Body);
        return (Expression<Func<T, bool>>)Expression.Lambda(tupleBody, tupleParameter);
    }
}
