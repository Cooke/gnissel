#region

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Cooke.Gnissel.Utils;

#endregion

namespace Cooke.Gnissel.Services.Implementations;

public class DefaultObjectReaderProvider(IDbAdapter dbAdapter) : IObjectReaderProvider
{
    private readonly ConcurrentDictionary<Type, object> _readers = new();

    public ObjectReader<TOut> Get<TOut>(DbRuntimeContext dbRuntimeContext)
    {
        return (ObjectReader<TOut>)GetReader(typeof(TOut), dbRuntimeContext);
    }

    private object GetReader(Type type, DbRuntimeContext dbRuntimeContext) =>
        _readers.GetOrAdd(type, _ => CreateReader(type, dbRuntimeContext));

    private object CreateReader(Type type, DbRuntimeContext dbRuntimeContext)
    {
        var dataReader = Expression.Parameter(typeof(DbDataReader));
        var ordinalOffset = Expression.Parameter(typeof(int));
        var (body, width) = CreateReader(
            dataReader,
            ordinalOffset,
            type,
            ImmutableList<ObjectPathPart>.Empty,
            dbRuntimeContext
        );
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
        Type type,
        IImmutableList<ObjectPathPart> parameterChain,
        DbRuntimeContext dbRuntimeContext
    )
    {
        if (IsNullableValueType(type))
        {
            var underlyingType = Nullable.GetUnderlyingType(type)!;
            var (actualReader, width) = CreateReader(
                dataReader,
                ordinalOffset,
                underlyingType,
                parameterChain,
                dbRuntimeContext
            );
            return (
                Expression.Condition(
                    CreateIsNullReader(dataReader, ordinalOffset, underlyingType, parameterChain),
                    Expression.Constant(null, type),
                    Expression.New(type.GetConstructor([underlyingType])!, [actualReader])
                ),
                width
            );
        }

        if (dbRuntimeContext.TryGetConverter(type, out var converter))
        {
            var ordinal = GetOrdinalByMemberChain(
                dataReader,
                ordinalOffset,
                dbAdapter,
                parameterChain
            );
            return (
                Expression.Call(
                    Expression.Constant(converter),
                    "FromReader",
                    [],
                    dataReader,
                    ordinal
                ),
                1
            );
        }

        if (type.GetWrappedType() is { } wrappedType)
        {
            var ordinal = GetOrdinalByMemberChain(
                dataReader,
                ordinalOffset,
                dbAdapter,
                parameterChain
            );
            var constructorInfo = type.GetConstructors().First(x => x.GetParameters().Length == 1);
            return (
                Expression.New(
                    constructorInfo,
                    CreateValueReader(dataReader, ordinal, wrappedType)
                ),
                1
            );
        }

        if (dbAdapter.IsDbMapped(type))
        {
            var ordinal = GetOrdinalByMemberChain(
                dataReader,
                ordinalOffset,
                dbAdapter,
                parameterChain
            );
            return (CreateValueReader(dataReader, ordinal, type), 1);
        }

        if (type.IsAssignableTo(typeof(ITuple)))
        {
            return CreatePositionalReader(dataReader, ordinalOffset, type, parameterChain);
        }

        if (type.IsClass)
        {
            var (actualReader, width) = CreateNamedReader(
                dataReader,
                ordinalOffset,
                type,
                parameterChain
            );
            return (
                Expression.Condition(
                    CreateIsNullReader(dataReader, ordinalOffset, type, parameterChain),
                    Expression.Constant(null, type),
                    actualReader
                ),
                width
            );
        }

        if (type.IsValueType)
        {
            return CreateNamedReader(
                dataReader,
                ordinalOffset,
                type,
                ImmutableList<ObjectPathPart>.Empty
            );
        }

        throw new NotSupportedException($"Cannot map type {type}");
    }

    private Expression CreateIsNullReader(
        Expression dataReader,
        Expression ordinalOffset,
        Type type,
        IImmutableList<ObjectPathPart> parameterChain
    )
    {
        if (dbAdapter.IsDbMapped(type))
        {
            return CreateIsNullValueReader(dataReader, ordinalOffset);
        }

        if (type.IsAssignableTo(typeof(ITuple)) || type.IsClass)
        {
            return CreateIsNullComplexReader(dataReader, ordinalOffset, type, parameterChain);
        }

        throw new NotSupportedException($"Cannot create is null reader for type {type}");
    }

    private Expression CreateIsNullComplexReader(
        Expression dataReader,
        Expression ordinalOffset,
        Type type,
        IImmutableList<ObjectPathPart> parameterChain
    )
    {
        var ctor = type.GetConstructors().First();
        return ctor.GetParameters()
            .Select(p =>
                CreateIsNullReader(
                    dataReader,
                    ordinalOffset,
                    p.ParameterType,
                    parameterChain.Add(new ParameterPathPart(p))
                )
            )
            .Aggregate((Expression)Expression.Constant(true), Expression.And);
    }

    private (Expression Body, int Width) CreateNamedReader(
        Expression dataReader,
        Expression ordinalOffset,
        Type type,
        IImmutableList<ObjectPathPart> parameterChain
    )
    {
        var ctor = type.GetConstructors().First();
        var width = 0;

        var body = Expression.New(
            ctor,
            ctor.GetParameters()
                .Select(p =>
                {
                    var (body, innerWidth) = CreateReader(
                        dataReader,
                        ordinalOffset,
                        p.ParameterType,
                        parameterChain.Add(new ParameterPathPart(p))
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
        Type type,
        IImmutableList<ObjectPathPart> parameterChain
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
                        p.ParameterType,
                        parameterChain
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

    private static Expression GetOrdinalByMemberChain(
        Expression dataReader,
        Expression ordinalOffset,
        IDbAdapter dbAdapter,
        IImmutableList<ObjectPathPart> parameterChain
    )
    {
        if (parameterChain.Count == 0)
        {
            return ordinalOffset;
        }

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
            Expression.Constant(dbAdapter.ToColumnName(parameterChain))
        );
    }

    private static int GetOrdinalAfter(DbDataReader dataReader, int offset, string name)
    {
        for (int ordinal = offset; ordinal < dataReader.FieldCount; ordinal++)
        {
            if (dataReader.GetName(ordinal).Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return ordinal;
            }
        }

        throw new InvalidOperationException(
            $"No column with name '{name}' in data reader after offset {offset}"
        );
    }

    private static bool IsNullableValueType(Type type) =>
        type.IsValueType && Nullable.GetUnderlyingType(type) != null;
}
