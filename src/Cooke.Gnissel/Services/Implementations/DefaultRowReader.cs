#region

using System.Collections.Concurrent;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using Cooke.Gnissel.Utils;

#endregion

namespace Cooke.Gnissel.Services.Implementations;

public class DefaultRowReader : IRowReader
{
    private readonly ConcurrentDictionary<Type, object> _readers =
        new ConcurrentDictionary<Type, object>();

    public TOut Read<TOut>(DbDataReader rowReader) =>
        ((Func<DbDataReader, TOut>)_readers.GetOrAdd(typeof(TOut), _ => CreateReader<TOut>()))(
            rowReader
        );

    private static Func<DbDataReader, TOut> CreateReader<TOut>()
    {
        var row = Expression.Parameter(typeof(DbDataReader));
        return Expression
            .Lambda<Func<DbDataReader, TOut>>(CreateReader(row, typeof(TOut)), row)
            .Compile();
    }

    private static Expression CreateReader(Expression rowReader, Type type, string? dbType = null)
    {
        if (type.GetDbType() != null || dbType != null)
        {
            return GetFieldValue(rowReader, type, Expression.Constant(0));
        }

        return type switch
        {
            { IsValueType: true } => CreatePositionalReader(rowReader, type),
            not null when type == typeof(string) => CreatePositionalReader(rowReader, type),
            { IsClass: true } => CreateNamedReader(rowReader, type),
            _ => throw new NotSupportedException($"Cannot map type {type}")
        };
    }

    private static Expression CreateNamedReader(Expression rowReader, Type type)
    {
        var ctor = type.GetConstructors().First();
        return Expression.New(
            ctor,
            ctor.GetParameters()
                .Select(p => GetFieldValue(rowReader, p.ParameterType, GetOrdinal(rowReader, p)))
        );
    }

    private static Expression CreatePositionalReader(Expression rowReader, Type type)
    {
        var ctor = type.GetConstructors().First();
        return Expression.New(
            ctor,
            ctor.GetParameters()
                .Select((p, i) => GetFieldValue(rowReader, p.ParameterType, Expression.Constant(i)))
        );
    }

    private static MethodCallExpression GetFieldValue(
        Expression rowReader,
        Type type,
        Expression ordinal
    ) => Expression.Call(rowReader, "GetFieldValue", new[] { type }, ordinal);

    private static MethodCallExpression GetOrdinal(Expression rowReader, string name) =>
        Expression.Call(rowReader, "GetOrdinal", Type.EmptyTypes, Expression.Constant(name));

    private static MethodCallExpression GetOrdinal(Expression rowReader, ParameterInfo p) =>
        GetOrdinal(
            rowReader,
            p.Name ?? throw new NotSupportedException($"Cannot map property without name: {p}")
        );
}
