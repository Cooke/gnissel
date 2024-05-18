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
    public static ImmutableArray<Column<T>> CreateColumns<T>(TableOptions options) =>
        [.. typeof(T).GetProperties().SelectMany(p => CreateColumns<T>(options, [p]))];

    private static IEnumerable<Column<T>> CreateColumns<T>(
        TableOptions options,
        IReadOnlyList<PropertyInfo> memberChain
    )
    {
        var member = memberChain[^1];
        var memberType = member.PropertyType;
        if (options.Ignores.Any(x => x.SequenceEqual(memberChain)))
            yield break;

        var converter = options.DbOptions.GetConverter(memberType);
        if (converter != null)
        {
            var columnOptions = options.Columns.FirstOrDefault(x =>
                x.MemberChain.SequenceEqual(memberChain)
            );
            var columnName =
                columnOptions?.Name
                ?? member.GetDbName()
                ?? options.DbOptions.DbAdapter.ToColumnName(
                    memberChain.Select(x => new PropertyPathPart(x))
                );

            var objectExpression = Expression.Parameter(typeof(T));
            var paramFactory = Expression
                .Lambda<Func<T, DbParameter>>(
                    Expression.Call(
                        Expression.Constant(
                            converter,
                            typeof(DbConverter<>).MakeGenericType(memberType)
                        ),
                        nameof(DbConverter<object>.ToParameter),
                        [],
                        objectExpression.ToMemberExpression(memberChain),
                        Expression.Constant(options.DbOptions.DbAdapter)
                    ),
                    objectExpression
                )
                .Compile();

            yield return new Column<T>(
                columnName,
                memberChain,
                paramFactory,
                member.GetCustomAttribute<DatabaseGeneratedAttribute>() != null
            );
        }
        else if (options.DbOptions.DbAdapter.IsDbMapped(memberType))
        {
            yield return CreateColumn<T>(options, memberChain);
        }
        else if (memberType.IsClass)
        {
            foreach (
                var column in memberType
                    .GetProperties()
                    .SelectMany<PropertyInfo, Column<T>>(innerProperty =>
                        CreateColumns<T>(options, [.. memberChain, innerProperty])
                    )
            )
            {
                yield return column;
            }
        }
        else
        {
            throw new Exception("Not supported column type.");
        }
    }

    private static Column<T> CreateColumn<T>(
        TableOptions options,
        IReadOnlyList<PropertyInfo> memberChain
    )
    {
        var p = memberChain.Last();
        var columnOptions = options.Columns.FirstOrDefault(x =>
            x.MemberChain.SequenceEqual(memberChain)
        );
        return new Column<T>(
            columnOptions?.Name
                ?? p.GetDbName()
                ?? options.DbOptions.DbAdapter.ToColumnName(
                    memberChain.Select(x => new PropertyPathPart(x))
                ),
            memberChain,
            CreateParameterFactory<T>(options.DbOptions.DbAdapter, memberChain),
            p.GetCustomAttribute<DatabaseGeneratedAttribute>() != null
        );
    }

    private static Func<T, DbParameter> CreateParameterFactory<T>(
        IDbAdapter dbAdapter,
        IReadOnlyList<PropertyInfo> memberChain
    )
    {
        var rootObjectExpression = Expression.Parameter(typeof(T));
        var propertyInfo = memberChain[^1];
        return Expression
            .Lambda<Func<T, DbParameter>>(
                Expression.Call(
                    Expression.Constant(dbAdapter),
                    nameof(dbAdapter.CreateParameter),
                    [propertyInfo.PropertyType],
                    rootObjectExpression.ToMemberExpression(memberChain),
                    Expression.Constant(propertyInfo.GetDbType(), typeof(string))
                ),
                rootObjectExpression
            )
            .Compile();
    }
}
