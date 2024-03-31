using System.Reflection;

namespace Cooke.Gnissel.Services;

public interface IIdentifierMapper
{
    string ToColumnName(IEnumerable<IdentifierPart> path);

    string ToTableName(Type type);

    public abstract record IdentifierPart;

    public record ParameterPart(ParameterInfo ParameterInfo) : IdentifierPart;

    public record PropertyPart(PropertyInfo PropertyInfo) : IdentifierPart;
}
