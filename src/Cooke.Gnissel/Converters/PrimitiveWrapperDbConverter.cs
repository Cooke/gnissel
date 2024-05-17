using System.Data.Common;
using System.Linq.Expressions;
using Cooke.Gnissel.Services;

namespace Cooke.Gnissel.Converters;

public class PrimitiveWrapperDbConverter : DbConverterFactory
{
    public override bool CanCreateFor(Type type)
    {
        return type.GetConstructors().Any(x => x.GetParameters().Length == 1)
            && type.GetProperties().Any();
    }

    public override IDbConverter Create(Type type)
    {
        var propertyInfo = type.GetProperties().Single();
        var innerType = propertyInfo.PropertyType;

        var objectExpression = Expression.Parameter(type);
        var adapterExpression = Expression.Parameter(typeof(IDbAdapter));

        var toParameter = Expression
            .Lambda(
                Expression.Call(
                    adapterExpression,
                    nameof(IDbAdapter.CreateParameter),
                    [innerType],
                    Expression.Property(objectExpression, propertyInfo),
                    Expression.Constant(null, typeof(string))
                ),
                [objectExpression, adapterExpression]
            )
            .Compile();

        var readerExpression = Expression.Parameter(typeof(DbDataReader));
        var ordinalExpression = Expression.Parameter(typeof(int));
        var fromReader = Expression
            .Lambda(
                Expression.New(
                    type.GetConstructors().Single(x => x.GetParameters().Length == 1),
                    Expression.Call(
                        readerExpression,
                        nameof(DbDataReader.GetFieldValue),
                        [innerType],
                        ordinalExpression
                    )
                ),
                readerExpression,
                ordinalExpression
            )
            .Compile();

        return (IDbConverter)
            Activator.CreateInstance(
                typeof(ConcreteConverter<>).MakeGenericType(type),
                toParameter,
                fromReader
            )!;
    }

    private class ConcreteConverter<T>(
        Func<T, IDbAdapter, DbParameter> toParameter,
        Func<DbDataReader, int, T> fromReader
    ) : DbConverter<T>
    {
        public override DbParameter ToParameter(T value, IDbAdapter adapter) =>
            toParameter(value, adapter);

        public override T FromReader(DbDataReader reader, int ordinal) =>
            fromReader(reader, ordinal);
    }
}
