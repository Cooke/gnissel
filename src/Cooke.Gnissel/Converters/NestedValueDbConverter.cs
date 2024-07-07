using System.Data.Common;
using System.Linq.Expressions;

namespace Cooke.Gnissel.Converters;

public class NestedValueDbConverter : ConcreteDbConverterFactory
{
    public override bool CanCreateFor(Type type)
    {
        var ctor = type.GetConstructors().FirstOrDefault(x => x.GetParameters().Length == 1);
        return ctor != null
            && type.GetProperties()
                .Any(x => x.PropertyType == ctor.GetParameters().Single().ParameterType);
    }

    public override ConcreteDbConverter Create(Type type)
    {
        var propertyInfo = type.GetProperties().Single();
        var innerType = propertyInfo.PropertyType;

        var objectExpression = Expression.Parameter(type);

        var toDbValue = Expression
            .Lambda(
                Expression.New(
                    typeof(DbValue<>).MakeGenericType(innerType).GetConstructors().Single(),
                    Expression.Property(objectExpression, propertyInfo),
                    Expression.Constant(null, typeof(string))
                ),
                [objectExpression]
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

        return (ConcreteDbConverter)
            Activator.CreateInstance(
                typeof(ConcreteConverter<>).MakeGenericType(type),
                toDbValue,
                fromReader
            )!;
    }

    private class ConcreteConverter<T>(
        Func<T, DbValue> toDbValue,
        Func<DbDataReader, int, T> fromReader
    ) : ConcreteDbConverter<T>
    {
        public override DbValue ToValue(T value) => toDbValue(value);

        public override T FromReader(DbDataReader reader, int ordinal) =>
            fromReader(reader, ordinal);
    }
}
