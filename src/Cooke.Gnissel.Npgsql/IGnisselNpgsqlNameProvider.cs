using Cooke.Gnissel.Services;

namespace Cooke.Gnissel.Npgsql;

public interface IGnisselNpgsqlNameProvider
{
    string ToColumnName(PathSegment path);

    string ToTableName(Type type);
}
