using Cooke.Gnissel.Services;

namespace Cooke.Gnissel.Npgsql;

public interface IGnisselNpgsqlNameProvider
{
    string ToColumnName(IEnumerable<PathSegment> path);

    string ToTableName(Type type);
}
