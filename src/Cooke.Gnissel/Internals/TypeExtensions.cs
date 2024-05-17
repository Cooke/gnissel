#region

using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using Cooke.Gnissel.Services;

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

    public static string? GetDbName(this Type type) =>
        type.GetCustomAttribute<DbNameAttribute>()?.DbName;

    public static string? GetDbName(this PropertyInfo propInfo) =>
        propInfo.GetCustomAttribute<DbNameAttribute>()?.DbName ?? propInfo.PropertyType.GetDbName();

    public static string? GetDbName(this ParameterInfo paramInfo) =>
        paramInfo.GetCustomAttribute<DbNameAttribute>()?.DbName
        ?? paramInfo.ParameterType.GetDbName();

    public static string? GetDbName(this MemberInfo memberInfo) =>
        memberInfo switch
        {
            PropertyInfo propInfo => propInfo.GetDbName(),
            _ => null
        };
}
