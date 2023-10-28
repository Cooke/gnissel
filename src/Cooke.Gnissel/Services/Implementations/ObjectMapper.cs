#region

using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;

#endregion

namespace Cooke.Gnissel;

public class ObjectMapper : IObjectMapper
{
    private readonly IObjectMapperValueReader _objectMapperValueReader;

    private readonly ConcurrentDictionary<Type, object> _mappers =
        new ConcurrentDictionary<Type, object>();

    public ObjectMapper(IObjectMapperValueReader objectMapperValueReader)
    {
        _objectMapperValueReader = objectMapperValueReader;
    }

    public TOut Map<TOut>(DbDataReader rowReader) =>
        ((Func<DbDataReader, TOut>)_mappers.GetOrAdd(typeof(TOut), _ => CreateTypeMapper<TOut>()))(
            rowReader
        );

    private Func<DbDataReader, TOut> CreateTypeMapper<TOut>()
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

    private Func<DbDataReader, TOut> CreateNamedMapper<TOut>()
    {
        var type = typeof(TOut);
        var ctor = type.GetConstructors().First();
        return CreateMapper<TOut>(
            rowReader =>
                Expression.New(
                    ctor,
                    ctor.GetParameters()
                        .Select(p =>
                        {
                            var attr = p.GetCustomAttribute<DataTypeAttribute>();
                            var dbType = attr?.CustomDataType;
                            var readValue = Expression.Call(
                                Expression.Constant(_objectMapperValueReader),
                                "Read",
                                new[] { p.ParameterType },
                                rowReader,
                                GetOrdinal(rowReader, p.Name),
                                Expression.Constant(dbType, typeof(string))
                            );
                            return readValue;
                        })
                )
        );
    }

    private static Func<DbDataReader, TOut> CreatePositionalMapper<TOut>()
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

    private static Func<DbDataReader, TOut> CreateMapper<TOut>(
        Func<ParameterExpression, NewExpression> bodyExpression
    )
    {
        var row = Expression.Parameter(typeof(DbDataReader));
        return Expression.Lambda<Func<DbDataReader, TOut>>(bodyExpression(row), row).Compile();
    }

    private static MethodCallExpression GetColumnByPosition(Expression row, Type type, int i) =>
        Expression.Call(row, "GetFieldValue", new[] { type }, Expression.Constant(i));

    private static MethodCallExpression GetOrdinal(Expression row, string name) =>
        Expression.Call(row, "GetOrdinal", Type.EmptyTypes, Expression.Constant(name));
}
