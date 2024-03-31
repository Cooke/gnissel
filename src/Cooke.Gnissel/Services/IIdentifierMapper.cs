using System.Reflection;

namespace Cooke.Gnissel.Services;

public interface IIdentifierMapper
{
    string ToColumnName(IEnumerable<ObjectPathPart> path);

    string ToTableName(Type type);
}

