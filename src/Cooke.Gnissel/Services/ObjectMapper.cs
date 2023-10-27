using System.Collections.Concurrent;
using System.Data.Common;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Cooke.Gnissel;

public class ObjectMapper : IObjectMapper
{
    private readonly ConcurrentDictionary<Type, object> _mappers =
        new ConcurrentDictionary<Type, object>();

    public TOut Map<TOut>(RowReader rowReader)
    {
        var mapper =
            (Func<RowReader, TOut>)_mappers.GetOrAdd(typeof(TOut), _ => CreateTypeMapper<TOut>());
        return mapper(rowReader);
    }

    private static Func<RowReader, TOut> CreateTypeMapper<TOut>()
    {
        var type = typeof(TOut);
        return type switch
        {
            { IsValueType: true } => CreatePositionalMapper<TOut>(),
            not null when type == typeof(string) => CreatePositionalMapper<TOut>(),
            { IsClass: true } => CreateNamedMapper<TOut>(),
            _ => throw new NotSupportedException($"Cannot map type {type.Name}")
        };
    }

    private static Func<RowReader, TOut> CreateNamedMapper<TOut>()
    {
        var type = typeof(TOut);
        var ctor = type.GetConstructors().First();
        return CreateMapper<TOut>(
            row =>
                Expression.New(
                    ctor,
                    ctor.GetParameters().Select(p => GetColumnByName(row, p.ParameterType, p.Name))
                )
        );
    }

    private static Func<RowReader, TOut> CreatePositionalMapper<TOut>()
    {
        var ctor = typeof(TOut).GetConstructors().First();
        return CreateMapper<TOut>(
            row =>
                Expression.New(
                    ctor,
                    ctor.GetParameters()
                        .Select((p, i) => GetColumnByPosition(row, p.ParameterType, i))
                )
        );
    }

    private static Func<RowReader, TOut> CreateMapper<TOut>(
        Func<ParameterExpression, NewExpression> bodyExpression
    )
    {
        var row = Expression.Parameter(typeof(RowReader));
        return Expression.Lambda<Func<RowReader, TOut>>(bodyExpression(row), row).Compile();
    }

    private static MethodCallExpression GetColumnByPosition(Expression row, Type type, int i) =>
        Expression.Call(row, "Get", new[] { type }, Expression.Constant(i));

    private static MethodCallExpression GetColumnByName(Expression row, Type type, string name) =>
        Expression.Call(row, "Get", new[] { type }, Expression.Constant(name));
}
