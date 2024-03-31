using System.Globalization;
using System.Reflection;
using Cooke.Gnissel.Services;
using Npgsql.NameTranslation;

namespace Cooke.Gnissel.Npgsql;

public class DefaultPostgresIdentifierMapper : IIdentifierMapper
{
    private static readonly Type[] BuiltInTypes =
    [
        typeof(string),
        typeof(DateTime),
        typeof(DateTimeOffset),
        typeof(TimeSpan),
        typeof(Guid),
        typeof(byte[])
    ];

    private static bool IsValueType(Type type)
    {
        return type.IsPrimitive || BuiltInTypes.Contains(type);
    }

    public string ToColumnName(ParameterInfo parameterInfo) =>
        ConvertToSnakeCase(parameterInfo.Name);

    public string ToColumnName(PropertyInfo propertyInfo) => ConvertToSnakeCase(propertyInfo.Name);

    public string ToColumnName(IEnumerable<IIdentifierMapper.IdentifierPart> path) =>
        string.Join(
            ".",
            path.Where(x => !(x is IIdentifierMapper.ParameterPart
            {
                ParameterInfo.Member: ConstructorInfo ctor
            } param && ctor.GetParameters().Length == 1 && IsValueType(param.ParameterInfo.ParameterType))).Select(
                part =>
                    part switch
                    {
                        IIdentifierMapper.ParameterPart parameterPart
                            => ToColumnName(parameterPart.ParameterInfo),
                        IIdentifierMapper.PropertyPart propertyPart
                            => ToColumnName(propertyPart.PropertyInfo),
                        _ => throw new InvalidOperationException()
                    }
            )
        );

    public string ToTableName(Type type) => ConvertToSnakeCase(type.Name) + "s";

    private static string ConvertToSnakeCase(string? name)
    {
        return NpgsqlSnakeCaseNameTranslator.ConvertToSnakeCase(
            name ?? throw new InvalidOperationException(),
            CultureInfo.CurrentCulture
        );
    }
}