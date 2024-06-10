using Cooke.Gnissel.Services;

namespace Cooke.Gnissel.Npgsql;

public interface IGnisselNpgsqlNameProvider
{
    string ToColumnName(IEnumerable<ObjectPathPart> path);

    string ToTableName(Type type);
}
