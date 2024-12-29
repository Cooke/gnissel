using Cooke.Gnissel.Services;

namespace Cooke.Gnissel.Npgsql;

public interface IGnisselNpgsqlNameProvider
{
    string ToColumnName(IEnumerable<string> path);

    string ToTableName(Type type);
}
