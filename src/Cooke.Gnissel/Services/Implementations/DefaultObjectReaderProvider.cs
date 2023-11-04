#region

using System.Collections.Concurrent;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Cooke.Gnissel.Utils;

#endregion

namespace Cooke.Gnissel.Services.Implementations;

public class DefaultObjectReaderProvider : IObjectReaderProvider
{
    private readonly IIdentifierMapper _identifierMapper;
    private readonly ConcurrentDictionary<Type, object> _readers =
        new ConcurrentDictionary<Type, object>();

    public DefaultObjectReaderProvider(IIdentifierMapper identifierMapper)
    {
        _identifierMapper = identifierMapper;
    }

    public ObjectReader<TOut> GetReader<TOut>() => (ObjectReader<TOut>)GetReader(typeof(TOut));

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
        Type type,
        string? dbType = null
    )
    {
        if (type.GetDbType() != null || dbType != null)
        {
            return (CreateValueReader(dataReader, ordinalOffset, type), 1);
        }

        return type switch
        {
            { IsPrimitive: true } => (CreateValueReader(dataReader, ordinalOffset, type), 1),
            { IsValueType: true } => CreatePositionalReader(dataReader, ordinalOffset, type),
            not null when type == typeof(string)
                => (CreateValueReader(dataReader, ordinalOffset, typeof(string)), 1),
            { IsClass: true } => CreateNamedReader(dataReader, ordinalOffset, type),
            _ => throw new NotSupportedException($"Cannot map type {type}")
        };
    }

    private (Expression Body, int Width) CreateNamedReader(
        Expression dataReader,
        Expression ordinalOffset,
        Type type
    )
    {
        var ctor = type.GetConstructors().First();
        var width = ctor.GetParameters().Length;
        var body = Expression.New(
            ctor,
            ctor.GetParameters()
                .Select(
                    p =>
                        CreateValueReader(
                            dataReader,
                            GetOrdinal(dataReader, ordinalOffset, width, _identifierMapper.ToColumnName(p)),
                            p.ParameterType
                        )
                )
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
                    var (reader, width) = ((Expression Reader, int Width))CreateReader(
                        dataReader,
                        Expression.Add(ordinalOffset, Expression.Constant(totalWidth)),
                        p.ParameterType
                    );
                    totalWidth += width;
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

    private static Expression GetOrdinal(
        Expression dataReader,
        Expression ordinalOffset,
        int width,
        string name
    )
    {
        var getOrdinalInMethod =
            typeof(DefaultObjectReaderProvider).GetMethod(
                nameof(GetOrdinalIn),
                BindingFlags.Static | BindingFlags.NonPublic
            )
            ?? throw new ArgumentNullException(
                "typeof(DefaultObjectReaderProvider).GetMethod(nameof(GetOrdinalIn))"
            );
        return Expression.Call(
            getOrdinalInMethod,
            dataReader,
            ordinalOffset,
            Expression.Constant(width),
            Expression.Constant(name)
        );
    }

    private static int GetOrdinalIn(DbDataReader dataReader, int offset, int width, string name)
    {
        for (int ordinal = offset; ordinal < offset + width; ordinal++)
        {
            if (dataReader.GetName(ordinal).Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return ordinal;
            }
        }

        throw new InvalidOperationException(
            $"No column with name '{name}' in data reader between ordinals {offset} and {offset + width}"
        );
    }
}
