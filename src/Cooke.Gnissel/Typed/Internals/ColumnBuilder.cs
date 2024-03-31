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
    public static ImmutableArray<Column<T>> CreateColumns<T>(TableOptions options)
    {
        var objectParameter = Expression.Parameter(typeof(T));
        return typeof(T)
            .GetProperties()
            .SelectMany(
                p =>
                    CreateColumns<T>(
                        options,
                        p,
                        objectParameter,
                        Expression.Property(objectParameter, p)
                    )
            )
            .ToImmutableArray();
    }

    private static IEnumerable<Column<T>> CreateColumns<T>(TableOptions options,
        PropertyInfo p,
        ParameterExpression rootExpression,
        Expression memberExpression)
    {
        var methodChain = ExpressionUtils.GetMemberChain(memberExpression);
        if (options.Ignores.Any(x => x.SequenceEqual(methodChain)))
            yield break;

        if (p.GetDbType() != null)
            yield return CreateColumn<T>(options, p, rootExpression, memberExpression);
        else if (p.PropertyType == typeof(string) || p.PropertyType.IsPrimitive)
            yield return CreateColumn<T>(options, p, rootExpression, memberExpression);
        else if (p.PropertyType.IsClass)
            foreach (
                var column in p.PropertyType
                    .GetProperties()
                    .SelectMany<PropertyInfo, Column<T>>(
                        innerProperty =>
                            CreateColumns<T>(
                                options,
                                innerProperty,
                                rootExpression,
                                Expression.Property(memberExpression, innerProperty)
                            )
                    )
            )
                yield return column;
        else
            yield return CreateColumn<T>(options, p, rootExpression, memberExpression);
    }

    private static Column<T> CreateColumn<T>(TableOptions options,
        PropertyInfo p,
        ParameterExpression objectExpression,
        Expression memberExpression)
    {
        var memberChain = ExpressionUtils.GetMemberChain(memberExpression);
        var columnOptions = options.Columns.FirstOrDefault(x => x.MemberChain.SequenceEqual(memberChain));
        return new Column<T>(columnOptions?.Name ?? p.GetDbName() ?? options.DbOptions.DbAdapter.IdentifierMapper.ToColumnName(memberChain.Select(x => new IIdentifierMapper.PropertyPart((PropertyInfo)x))),
            memberChain,
            CreateParameterFactory<T>(
                options.DbOptions.DbAdapter,
                memberExpression,
                p.GetDbType(),
                objectExpression
            ),
            p.GetCustomAttribute<DatabaseGeneratedAttribute>() != null
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