using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations.Schema;
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

        if (options.DbOptions.GetConverter(memberType) != null)
        {
            yield return CreateColumn<T>(options, memberChain);
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
            throw new Exception($"Not supported column type: {memberType}.");
        }
    }

    private static Column<T> CreateColumn<T>(
        TableOptions options,
        IReadOnlyList<PropertyInfo> memberChain
    )
    {
        var property = memberChain[^1];
        var columnOptions = options.Columns.FirstOrDefault(x =>
            x.MemberChain.SequenceEqual(memberChain)
        );
        return new Column<T>(
            columnOptions?.Name
                ?? property.GetDbName()
                ?? options.DbOptions.DbAdapter.ToColumnName(
                    memberChain.Select(x => new PropertyPathPart(x))
                ),
            memberChain,
            CreateParameterFactory<T>(memberChain),
            property.GetCustomAttribute<DatabaseGeneratedAttribute>() != null
        );
    }

    private static Func<T, Sql.Parameter> CreateParameterFactory<T>(
        IReadOnlyList<PropertyInfo> memberChain
    )
    {
        var propertyInfo = memberChain[^1];
        var objectExpression = Expression.Parameter(typeof(T));
        return Expression
            .Lambda<Func<T, Sql.Parameter>>(
                Expression.Convert(
                    Expression.New(
                        typeof(Sql.Parameter<>)
                            .MakeGenericType(propertyInfo.PropertyType)
                            .GetConstructors()
                            .Single(),
                        objectExpression.ToMemberExpression(memberChain),
                        Expression.Constant(propertyInfo.GetDbType(), typeof(string))
                    ),
                    typeof(Sql.Parameter)
                ),
                objectExpression
            )
            .Compile();
    }
}
