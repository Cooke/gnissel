#region

using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Cooke.Gnissel.Utils;

#endregion

namespace Cooke.Gnissel.Services.Implementations;

public class DefaultObjectReaderProvider(IIdentifierMapper identifierMapper) : IObjectReaderProvider
{
    private readonly ConcurrentDictionary<Type, object> _readers =
        new ConcurrentDictionary<Type, object>();

    private static readonly Type[] BuiltInTypes =
    [
        typeof(string),
        typeof(DateTime),
        typeof(DateTimeOffset),
        typeof(TimeSpan),
        typeof(Guid),
        typeof(byte[])
    ];

    public ObjectReader<TOut> Get<TOut>()
    {
        return (ObjectReader<TOut>)GetReader(typeof(TOut));
    }

    private object GetReader(Type type) => _readers.GetOrAdd(type, _ => CreateReader(type));

    private object CreateReader(Type type)
    {
        var dataReader = Expression.Parameter(typeof(DbDataReader));
        var ordinalOffset = Expression.Parameter(typeof(int));
        var (body, width) = CreateReader(dataReader, ordinalOffset, type);
        var objectReader = Expression
            .Lambda(
                typeof(ObjectReaderFunc<>).MakeGenericType(type),
                body,
                dataReader,
                ordinalOffset
            )
            .Compile();
        return Activator.CreateInstance(
            typeof(ObjectReader<>).MakeGenericType(type),
            objectReader,
            width
        )!;
    }


    private (Expression Body, int Width) CreateReader(
        Expression dataReader,
        Expression ordinalOffset,
        Type type
    )
    {
        if (IsNullableValueType(type)) {
            var underlyingType = Nullable.GetUnderlyingType(type)!;
            var (actualReader, width) = CreateReader(dataReader, ordinalOffset, underlyingType);
            return (Expression.Condition(
                CreateIsNullReader(dataReader, ordinalOffset, underlyingType),
                Expression.Constant(null, type),
                Expression.New(type.GetConstructor([underlyingType])!, [actualReader])
            ), width);
        }

        if (type.GetDbType() != null || type.IsPrimitive || BuiltInTypes.Contains(type)) {
            return (CreateValueReader(dataReader, ordinalOffset, type), 1);
        }

        if (type.IsAssignableTo(typeof(ITuple))) {
            return CreatePositionalReader(dataReader, ordinalOffset, type);
        }

        if (type.IsClass) {
            var (actualReader, width) = CreateNamedReader(dataReader, ordinalOffset, type);
            return (Expression.Condition(
                CreateIsNullReader(dataReader, ordinalOffset, type),
                Expression.Constant(null, type),
                actualReader
            ), width);
        }

        if (type.IsValueType) {
            return CreateNamedReader(dataReader, ordinalOffset, type);
        }

        throw new NotSupportedException($"Cannot map type {type}");
    }

    private Expression CreateIsNullReader(
        Expression dataReader,
        Expression ordinalOffset,
        Type type
    )
    {
        if (type.GetDbType() != null || type.IsPrimitive || BuiltInTypes.Contains(type)) {
            return CreateIsNullValueReader(dataReader, ordinalOffset);
        }

        if (type.IsAssignableTo(typeof(ITuple)) || type.IsClass) {
            return CreateIsNullComplexReader(dataReader, ordinalOffset, type);
        }

        throw new NotSupportedException($"Cannot create is null reader for type {type}");
    }

    private Expression CreateIsNullComplexReader(
        Expression dataReader,
        Expression ordinalOffset,
        Type type
    )
    {
        var ctor = type.GetConstructors().First();
        return ctor.GetParameters().Select(p =>
                CreateIsNullReader(dataReader, GetOrdinalAfterExpression(dataReader, ordinalOffset, p.GetDbName() ?? identifierMapper.ToColumnName(p)), p.ParameterType))
            .Aggregate((Expression)Expression.Constant(true), Expression.And);
    }

    private (Expression Body, int Width) CreateNamedReader(
        Expression dataReader,
        Expression ordinalOffset,
        Type type
    )
    {
        var ctor = type.GetConstructors().First();
        var width = 0;

        var body = Expression.New(
            ctor,
            ctor.GetParameters()
                .Select(p =>
                {
                    var ordinalAfterExpression = GetOrdinalAfterExpression(dataReader, ordinalOffset, p.GetDbName() ?? identifierMapper.ToColumnName(p));
                    var (body, innerWidth) = p.GetDbType() != null
                        ? (CreateValueReader(dataReader, ordinalAfterExpression, p.ParameterType), 1)
                        : CreateReader(
                            dataReader,
                            ordinalAfterExpression,
                            p.ParameterType
                        );
                    width += innerWidth;
                    return body;
                })
        );
        return (body, width);
    }

    private (Expression Body, int Width) CreatePositionalReader(
        Expression dataReader,
        Expression ordinalOffset,
        Type type
    )
    {
        var ctor = type.GetConstructors().First();
        int totalWidth = 0;
        var body = Expression.New(
            ctor,
            ctor.GetParameters()
                .Select(p =>
                {
                    var (reader, innerWidth) = CreateReader(
                        dataReader,
                        Expression.Add(ordinalOffset, Expression.Constant(totalWidth)),
                        p.ParameterType
                    );
                    totalWidth += innerWidth;
                    return reader;
                })
        );
        return (body, totalWidth);
    }

    private static MethodCallExpression CreateValueReader(
        Expression rowReader,
        Expression ordinal,
        Type type
    ) => Expression.Call(rowReader, "GetFieldValue", new[] { type }, ordinal);

    private static MethodCallExpression CreateIsNullValueReader(
        Expression rowReader,
        Expression ordinal
    ) => Expression.Call(rowReader, "IsDBNull", [], ordinal);

    private static Expression GetOrdinalAfterExpression(
        Expression dataReader,
        Expression ordinalOffset,
        string name
    )
    {
        var getOrdinalInMethod =
            typeof(DefaultObjectReaderProvider).GetMethod(
                nameof(GetOrdinalAfter),
                BindingFlags.Static | BindingFlags.NonPublic
            )
            ?? throw new ArgumentNullException(
                "typeof(DefaultObjectReaderProvider).GetMethod(nameof(GetOrdinalIn))"
            );
        return Expression.Call(
            getOrdinalInMethod,
            dataReader,
            ordinalOffset,
            Expression.Constant(name)
        );
    }

    private static int GetOrdinalAfter(DbDataReader dataReader, int offset, string name)
    {
        for (int ordinal = offset; ordinal < dataReader.FieldCount; ordinal++) {
            if (dataReader.GetName(ordinal).Equals(name, StringComparison.OrdinalIgnoreCase)) {
                return ordinal;
            }
        }

        throw new InvalidOperationException(
            $"No column with name '{name}' in data reader after offset {offset}"
        );
    }

    private static bool IsNullableValueType(Type type) => type.IsValueType && Nullable.GetUnderlyingType(type) != null;

}