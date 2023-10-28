#region

using System.Reflection;

#endregion

namespace Cooke.Gnissel.Utils;

internal static class TypeExtensions
{
    public static string? GetDbType(this Type type) =>
        type.GetCustomAttribute<DbTypeAttribute>()?.DbType;

    public static string? GetDbType(this PropertyInfo propInfo) =>
        propInfo.GetCustomAttribute<DbTypeAttribute>()?.DbType ?? propInfo.PropertyType.GetDbType();

    public static string? GetDbType(this ParameterInfo paramInfo) =>
        paramInfo.GetCustomAttribute<DbTypeAttribute>()?.DbType
        ?? paramInfo.ParameterType.GetDbType();
}
