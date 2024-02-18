using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using Cooke.Gnissel;
using Cooke.Gnissel.Services;
using Cooke.Gnissel.Utils;

namespace PlusPlusLab;

internal static class ColumnBuilder
{
    public static ImmutableArray<Column<T>> CreateColumns<T>(DbOptions dbOptions, Table<T> table)
    {
        var objectParameter = Expression.Parameter(typeof(T));
        return typeof(T)
            .GetProperties()
            .SelectMany(
                p =>
                    CreateColumns(
                        dbOptions,
                        p,
                        objectParameter,
                        Expression.Property(objectParameter, p),
                        table
                    )
            )
            .ToImmutableArray();
    }

    private static IEnumerable<Column<T>> CreateColumns<T>(DbOptions dbOptions,
        PropertyInfo p,
        ParameterExpression rootExpression,
        Expression memberExpression, Table<T> table)
    {
        if (p.GetDbType() != null)
            yield return CreateColumn(dbOptions, p, rootExpression, memberExpression, table);
        else if (p.PropertyType == typeof(string) || p.PropertyType.IsPrimitive)
            yield return CreateColumn(dbOptions, p, rootExpression, memberExpression, table);
        else if (p.PropertyType.IsClass)
            foreach (
                var column in p.PropertyType
                    .GetProperties()
                    .SelectMany<PropertyInfo, Column<T>>(
                        innerProperty =>
                            CreateColumns(
                                dbOptions,
                                innerProperty,
                                rootExpression,
                                Expression.Property(memberExpression, innerProperty), table
                            )
                    )
            )
                yield return column;
        else
            yield return CreateColumn(dbOptions, p, rootExpression, memberExpression, table);
    }

    private static Column<T> CreateColumn<T>(DbOptions dbOptions,
        PropertyInfo p,
        ParameterExpression objectExpression,
        Expression memberExpression, Table<T> table)
    {
        return new Column<T>(
            table,
            p.GetDbName() ?? dbOptions.DbAdapter.DefaultIdentifierMapper.ToColumnName(p),
            p.GetCustomAttribute<DatabaseGeneratedAttribute>()
                ?.Let(x => x.DatabaseGeneratedOption == DatabaseGeneratedOption.Identity) ?? false,
            p,
            CreateParameterFactory<T>(
                dbOptions.DbAdapter,
                memberExpression,
                p.GetDbType(),
                objectExpression
            )
        );
    }

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