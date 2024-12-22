#region

using System.Collections.Concurrent;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

#endregion

namespace Cooke.Gnissel.Services.Implementations;

public class RuntimeGeneratedObjectReaderProvider(IDbAdapter dbAdapter) : IObjectReaderProvider
{
    private readonly ConcurrentDictionary<Type, object> _readers = new();

    public ObjectReader<TOut> Get<TOut>(DbOptions options)
    {
        return (ObjectReader<TOut>)
            _readers.GetOrAdd(
                typeof(TOut),
                () => (ObjectReader<TOut>)CreateReader(typeof(TOut), options)
            );
    }

    private object CreateReader(Type type, DbOptions options)
    {
        var dataReader = Expression.Parameter(typeof(DbDataReader));
        var ordinalOffset = Expression.Parameter(typeof(int));
        var (body, width) = CreateReader(dataReader, ordinalOffset, type, null, options);
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
        PathSegment? path,
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
            var actualReader = CreateValueReader(dataReader, ordinal, type);

            // Nullable
            if (type.IsClass)
            {
                return (
                    Expression.Condition(
                        CreateIsNullReader(dataReader, ordinal, type, path, options),
                        Expression.Constant(null, type),
                        actualReader
                    ),
                    1
                );
            }

            return (actualReader, 1);
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
        PathSegment? path,
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
            var ctor = GetReaderConstructor(type);
            return ctor.GetParameters()
                .Select(p =>
                    CreateIsNullReader(
                        dataReader,
                        ordinalOffset,
                        p.ParameterType,
                        PathSegment.Combine(
                            path,
                            new ParameterPathSegment(
                                p.Name ?? throw new InvalidOperationException()
                            )
                        ),
                        options
                    )
                )
                .Aggregate((Expression)Expression.Constant(true), Expression.And);
        }

        if (IsNullableValueType(type))
        {
            return CreateIsNullReader(
                dataReader,
                ordinalOffset,
                Nullable.GetUnderlyingType(type)!,
                path,
                options
            );
        }

        throw new NotSupportedException($"Cannot create is null reader for type {type}");
    }

    private (Expression Body, int Width) CreateNameReader(
        Expression dataReader,
        Expression ordinalOffset,
        Type type,
        PathSegment? path,
        DbOptions options
    )
    {
        var width = 0;
        var ctor = GetReaderConstructor(type);
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(prop =>
                prop.SetMethod?.IsPublic == true
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
                            PathSegment.Combine(
                                path,
                                new ParameterPathSegment(
                                    p.Name ?? throw new InvalidOperationException()
                                )
                            ),
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
                    PathSegment.Combine(path, new PropertyPathSegment(p.Name)),
                    options
                );
                width += innerWidth;
                return Expression.Bind(p, body);
            })
        );

        return (body, width);
    }

    public static ConstructorInfo GetReaderConstructor(Type type) =>
        type.GetConstructors().OrderByDescending(x => x.GetParameters().Length).FirstOrDefault()
        ?? throw new ArgumentException($"No valid constructor found for type {type}");

    private (Expression Body, int Width) CreatePositionalReader(
        Expression dataReader,
        Expression ordinalOffset,
        Type type,
        PathSegment path,
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
        PathSegment? path
    )
    {
        if (path == null)
        {
            return ordinalOffset;
        }

        var getOrdinalByNameMethod =
            typeof(RuntimeGeneratedObjectReaderProvider).GetMethod(
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
