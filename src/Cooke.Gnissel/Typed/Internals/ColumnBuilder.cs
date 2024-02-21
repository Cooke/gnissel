using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using Cooke.Gnissel.Services;
using Cooke.Gnissel.Utils;

namespace Cooke.Gnissel.Typed.Internals;

internal static class ColumnBuilder
{
    public static ImmutableArray<Column<T>> CreateColumns<T>(DbOptions dbOptions)
    {
        var objectParameter = Expression.Parameter(typeof(T));
        return typeof(T)
            .GetProperties()
            .SelectMany(
                p =>
                    CreateColumns<T>(
                        dbOptions,
                        p,
                        objectParameter,
                        Expression.Property(objectParameter, p)
                    )
            )
            .ToImmutableArray();
    }

    private static IEnumerable<Column<T>> CreateColumns<T>(DbOptions dbOptions,
        PropertyInfo p,
        ParameterExpression rootExpression,
        Expression memberExpression)
    {
        if (p.GetDbType() != null)
            yield return CreateColumn<T>(dbOptions, p, rootExpression, memberExpression);
        else if (p.PropertyType == typeof(string) || p.PropertyType.IsPrimitive)
            yield return CreateColumn<T>(dbOptions, p, rootExpression, memberExpression);
        else if (p.PropertyType.IsClass)
            foreach (
                var column in p.PropertyType
                    .GetProperties()
                    .SelectMany<PropertyInfo, Column<T>>(
                        innerProperty =>
                            CreateColumns<T>(
                                dbOptions,
                                innerProperty,
                                rootExpression,
                                Expression.Property(memberExpression, innerProperty)
                            )
                    )
            )
                yield return column;
        else
            yield return CreateColumn<T>(dbOptions, p, rootExpression, memberExpression);
    }

    private static Column<T> CreateColumn<T>(DbOptions dbOptions,
        PropertyInfo p,
        ParameterExpression objectExpression,
        Expression memberExpression) =>
        new Column<T>(p.GetDbName() ?? dbOptions.DbAdapter.IdentifierMapper.ToColumnName(p),
            p,
            CreateParameterFactory<T>(
                dbOptions.DbAdapter,
                memberExpression,
                p.GetDbType(),
                objectExpression
            ),
            p.GetCustomAttribute<DatabaseGeneratedAttribute>() != null
        );

    private static Func<T, DbParameter> CreateParameterFactory<T>(
        IDbAdapter dbAdapter,
        Expression valueExpression,
        string? dbType,
        ParameterExpression tableItemParameter
    )
    {
        return Expression
            .Lambda<Func<T, DbParameter>>(
                Expression.Call(
                    Expression.Constant(dbAdapter),
                    nameof(dbAdapter.CreateParameter),
                    [valueExpression.Type],
                    valueExpression,
                    Expression.Constant(dbType, typeof(string))
                ),
                tableItemParameter
            )
            .Compile();
    }
}