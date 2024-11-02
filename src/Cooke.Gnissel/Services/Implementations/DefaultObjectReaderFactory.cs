#region

using System.Collections.Immutable;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

#endregion

namespace Cooke.Gnissel.Services.Implementations;

public class DefaultObjectReaderFactory(IDbAdapter dbAdapter) : IObjectReaderFactory
{
    public ObjectReader<TOut> Create<TOut>(DbOptions options)
    {
        return (ObjectReader<TOut>)GetReader(typeof(TOut), options);
    }

    private object GetReader(Type type, DbOptions options) => CreateReader(type, options);

    private object CreateReader(Type type, DbOptions options)
    {
        var dataReader = Expression.Parameter(typeof(DbDataReader));
        var ordinalOffset = Expression.Parameter(typeof(int));
        var (body, width) = CreateReader(
            dataReader,
            ordinalOffset,
            type,
            ImmutableList<PathSegment>.Empty,
            options
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
        IImmutableList<PathSegment> path,
        DbOptions options
    )
    {
        if (IsNullableValueType(type))
        {
            var underlyingType = Nullable.GetUnderlyingType(type)!;
            var (underlyingTypeReader, width) = CreateReader(
                dataReader,
                ordinalOffset,
                underlyingType,
                path,
                options
            );
            return (
                Expression.Condition(
                    CreateIsNullReader(dataReader, ordinalOffset, underlyingType, path, options),
                    Expression.Constant(null, type),
                    Expression.New(type.GetConstructor([underlyingType])!, [underlyingTypeReader])
                ),
                width
            );
        }

        var converter = options.GetConverter(type);
        if (converter != null)
        {
            var ordinal = CreateGetOrdinalByName(dataReader, ordinalOffset, dbAdapter, path);
            var actualConvertReader = Expression.Call(
                Expression.Constant(converter, typeof(ConcreteDbConverter<>).MakeGenericType(type)),
                nameof(ConcreteDbConverter<object>.FromReader),
                [],
                dataReader,
                ordinal
            );

            // Classes are nullable
            if (type.IsClass)
            {
                return (
                    Expression.Condition(
                        CreateIsNullReader(dataReader, ordinal, type, path, options),
                        Expression.Constant(null, type),
                        actualConvertReader
                    ),
                    1
                );
            }

            return (actualConvertReader, 1);
        }

        if (dbAdapter.IsDbMapped(type))
        {
            var ordinal = CreateGetOrdinalByName(dataReader, ordinalOffset, dbAdapter, path);
            return (CreateValueReader(dataReader, ordinal, type), 1);
        }

        if (type.IsAssignableTo(typeof(ITuple)))
        {
            return CreatePositionalReader(dataReader, ordinalOffset, type, path, options);
        }

        if (type.IsClass)
        {
            var (actualReader, width) = CreateNameReader(
                dataReader,
                ordinalOffset,
                type,
                path,
                options
            );
            return (
                Expression.Condition(
                    CreateIsNullReader(dataReader, ordinalOffset, type, path, options),
                    Expression.Constant(null, type),
                    actualReader
                ),
                width
            );
        }

        if (type.IsValueType)
        {
            return CreateNameReader(dataReader, ordinalOffset, type, path, options);
        }

        throw new NotSupportedException($"Cannot map type {type}");
    }

    private Expression CreateIsNullReader(
        Expression dataReader,
        Expression ordinalOffset,
        Type type,
        IImmutableList<PathSegment> path,
        DbOptions options
    )
    {
        var converter = options.GetConverter(type);
        if (converter != null)
        {
            return Expression.Call(
                Expression.Constant(converter),
                nameof(ConcreteDbConverter.IsNull),
                [],
                dataReader,
                ordinalOffset
            );
        }

        if (dbAdapter.IsDbMapped(type))
        {
            var ordinal = CreateGetOrdinalByName(dataReader, ordinalOffset, dbAdapter, path);
            return CreateIsNullValueReader(dataReader, ordinal);
        }

        if (type.IsAssignableTo(typeof(ITuple)) || type.IsClass)
        {
            var ctor = type.GetConstructors().First();
            return ctor.GetParameters()
                .Select(p =>
                    CreateIsNullReader(
                        dataReader,
                        ordinalOffset,
                        p.ParameterType,
                        path.Add(new ParameterPathSegment(p)),
                        options
                    )
                )
                .Aggregate((Expression)Expression.Constant(true), Expression.And);
        }

        throw new NotSupportedException($"Cannot create is null reader for type {type}");
    }

    private (Expression Body, int Width) CreateNameReader(
        Expression dataReader,
        Expression ordinalOffset,
        Type type,
        IImmutableList<PathSegment> parameterChain,
        DbOptions options
    )
    {
        var width = 0;
        var ctor = type.GetConstructors().First();
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(prop =>
                prop.CanWrite
                && !ctor.GetParameters()
                    .Select(x => x.Name)
                    .Contains(prop.Name, StringComparer.InvariantCultureIgnoreCase)
            );

        var body = Expression.MemberInit(
            Expression.New(
                ctor,
                ctor.GetParameters()
                    .Select(p =>
                    {
                        var (body, innerWidth) = CreateReader(
                            dataReader,
                            ordinalOffset,
                            p.ParameterType,
                            parameterChain.Add(new ParameterPathSegment(p)),
                            options
                        );
                        width += innerWidth;
                        return body;
                    })
            ),
            props.Select(p =>
            {
                var (body, innerWidth) = CreateReader(
                    dataReader,
                    ordinalOffset,
                    p.PropertyType,
                    parameterChain.Add(new PropertyPathSegment(p)),
                    options
                );
                width += innerWidth;
                return Expression.Bind(p, body);
            })
        );

        return (body, width);
    }

    private (Expression Body, int Width) CreatePositionalReader(
        Expression dataReader,
        Expression ordinalOffset,
        Type type,
        IImmutableList<PathSegment> path,
        DbOptions options
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
                        path,
                        options
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
    ) => Expression.Call(rowReader, nameof(DbDataReader.GetFieldValue), [type], ordinal);

    private static MethodCallExpression CreateIsNullValueReader(
        Expression rowReader,
        Expression ordinal
    ) => Expression.Call(rowReader, nameof(DbDataReader.IsDBNull), [], ordinal);

    private static Expression CreateGetOrdinalByName(
        Expression dataReader,
        Expression ordinalOffset,
        IDbAdapter dbAdapter,
        IImmutableList<PathSegment> path
    )
    {
        if (path.Count == 0)
        {
            return ordinalOffset;
        }

        var getOrdinalByNameMethod =
            typeof(DefaultObjectReaderFactory).GetMethod(
                nameof(GetOrdinalByName),
                BindingFlags.Static | BindingFlags.NonPublic
            ) ?? throw new ArgumentNullException();
        return Expression.Call(
            getOrdinalByNameMethod,
            dataReader,
            ordinalOffset,
            Expression.Constant(dbAdapter.ToColumnName(path))
        );
    }

    private static int GetOrdinalByName(DbDataReader dataReader, int offset, string name)
    {
        for (int ordinal = offset; ordinal < dataReader.FieldCount; ordinal++)
        {
            if (dataReader.GetName(ordinal).Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return ordinal;
            }
        }

        throw new InvalidOperationException(
            $"No field with name '{name}' in data reader after offset {offset}"
        );
    }

    private static bool IsNullableValueType(Type type) =>
        type.IsValueType && Nullable.GetUnderlyingType(type) != null;
}
