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
            .SelectMany(p =>
                CreateColumns<T>(
                    options,
                    p,
                    objectParameter,
                    Expression.Property(objectParameter, p)
                )
            )
            .ToImmutableArray();
    }

    private static IEnumerable<Column<T>> CreateColumns<T>(
        TableOptions options,
        PropertyInfo p,
        ParameterExpression rootExpression,
        Expression memberExpression
    )
    {
        var methodChain = ExpressionUtils.GetMemberChain(memberExpression);
        if (options.Ignores.Any(x => x.SequenceEqual(methodChain)))
            yield break;

        if (p.PropertyType.GetWrappedType() is { } wrappedType)
        {
            var innerProperty = p
                .PropertyType.GetProperties()
                .Single(x => x.PropertyType == wrappedType);
            Expression innerMemberExpression = Expression.Property(memberExpression, innerProperty);
            yield return CreateColumn<T>(
                options,
                innerProperty,
                rootExpression,
                innerMemberExpression,
                ExpressionUtils.GetMemberChain(memberExpression)
            );
        }
        else if (options.DbOptions.DbAdapter.IsDbMapped(p.PropertyType))
        {
            yield return CreateColumn<T>(
                options,
                p,
                rootExpression,
                memberExpression,
                ExpressionUtils.GetMemberChain(memberExpression)
            );
        }
        else if (p.PropertyType.IsClass)
        {
            foreach (
                var column in p
                    .PropertyType.GetProperties()
                    .SelectMany<PropertyInfo, Column<T>>(innerProperty =>
                        CreateColumns<T>(
                            options,
                            innerProperty,
                            rootExpression,
                            Expression.Property(memberExpression, innerProperty)
                        )
                    )
            )
                yield return column;
        }
        else
        {
            yield return CreateColumn<T>(
                options,
                p,
                rootExpression,
                memberExpression,
                ExpressionUtils.GetMemberChain(memberExpression)
            );
        }
    }

    private static Column<T> CreateColumn<T>(
        TableOptions options,
        PropertyInfo p,
        ParameterExpression objectExpression,
        Expression memberExpression,
        IReadOnlyCollection<MemberInfo> memberChain
    )
    {
        var columnOptions = options.Columns.FirstOrDefault(x =>
            x.MemberChain.SequenceEqual(memberChain)
        );
        return new Column<T>(
            columnOptions?.Name
                ?? p.GetDbName()
                ?? options.DbOptions.DbAdapter.ToColumnName(
                    memberChain.Select(x => new PropertyPathPart((PropertyInfo)x))
                ),
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
