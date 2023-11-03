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
    private readonly ConcurrentDictionary<Type, object> _readers =
        new ConcurrentDictionary<Type, object>();

    public ObjectReader<TOut> GetReader<TOut>() =>
        (ObjectReader<TOut>)_readers.GetOrAdd(typeof(TOut), _ => CreateReader<TOut>());

    private static ObjectReader<TOut> CreateReader<TOut>()
    {
        var dataReader = Expression.Parameter(typeof(DbDataReader));
        var ordinalOffset = Expression.Parameter(typeof(int));
        // var ordinalCache = Expression.Parameter(typeof(int[]));
        var (body, width) = CreateReader(dataReader, ordinalOffset, typeof(TOut));
        var objectReader = Expression
            .Lambda<ObjectReaderFunc<TOut>>(body, dataReader, ordinalOffset)
            .Compile();
        return new ObjectReader<TOut>(objectReader, width);
    }

    private static (Expression Body, int Width) CreateReader(
        Expression dataReader,
        Expression ordinalOffset,
        // Expression cacheIndex,
        Type type,
        string? dbType = null
    )
    {
        if (type.GetDbType() != null || dbType != null)
        {
            return (GetFieldValue(dataReader, ordinalOffset, type), 1);
        }

        return type switch
        {
            { IsValueType: true } => CreatePositionalReader(dataReader, ordinalOffset, type),
            not null when type == typeof(string)
                => (GetFieldValue(dataReader, ordinalOffset, typeof(string)), 1),
            { IsClass: true } => CreateNamedReader(dataReader, ordinalOffset, type),
            _ => throw new NotSupportedException($"Cannot map type {type}")
        };
    }

    private static (Expression Body, int Width) CreateNamedReader(
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
                        GetFieldValue(
                            dataReader,
                            GetOrdinal(dataReader, ordinalOffset, width, p.Name),
                            p.ParameterType
                        )
                )
        );
        return (body, width);
    }

    private static (Expression Body, int Width) CreatePositionalReader(
        Expression rowReader,
        Expression ordinalOffset,
        Type type
    )
    {
        var ctor = type.GetConstructors().First();
        var body = Expression.New(
            ctor,
            ctor.GetParameters()
                .Select(
                    (p, i) =>
                        GetFieldValue(
                            rowReader,
                            Expression.Add(ordinalOffset, Expression.Constant(i)),
                            p.ParameterType
                        )
                )
        );
        return (body, ctor.GetParameters().Length);
    }

    private static MethodCallExpression GetFieldValue(
        Expression rowReader,
        Expression ordinal,
        Type type
    ) => Expression.Call(rowReader, "GetFieldValue", new[] { type }, ordinal);

    private static Expression GetOrdinal(
        Expression dataReader,
        Expression ordinalOffset,
        // Expression ordinalCache,
        // Expression cacheIndex,
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
        // var getOrdinalInMethod =
        //     typeof(DefaultObjectReaderProvider).GetMethod(nameof(GetOrdinalIn))
        //     ?? throw new ArgumentNullException(
        //         "typeof(DefaultObjectReaderProvider).GetMethod(nameof(GetOrdinalIn))"
        //     );
        // return Expression.IfThenElse(
        //     Expression.NotEqual(
        //         Expression.ArrayIndex(ordinalCache, cacheIndex),
        //         Expression.Constant(0)
        //     ),
        //     Expression.ArrayIndex(ordinalCache, cacheIndex),
        //     Expression.Assign(
        //         Expression.ArrayIndex(ordinalCache, cacheIndex),
        //         Expression.Call(
        //             getOrdinalInMethod,
        //             dataReader,
        //             ordinalOffset,
        //             Expression.Constant(width),
        //             Expression.Constant(name)
        //         )
        //     )
        // );
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
