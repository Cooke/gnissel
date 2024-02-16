using System.Reflection;

namespace Cooke.Gnissel.Services;

public interface IIdentifierMapper
{
    string ToColumnName(ParameterInfo parameterInfo);

    string ToColumnName(PropertyInfo propertyInfo);

    string ToTableName(Type type);
}
