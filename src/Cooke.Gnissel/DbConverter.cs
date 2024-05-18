using System.Data.Common;
using Cooke.Gnissel.Services;

namespace Cooke.Gnissel;

public abstract class DbConverter { }

public abstract class DbConverter<T> : DbConverter
{
    public abstract DbParameter ToParameter(T value, IDbAdapter adapter);

    public abstract T FromReader(DbDataReader reader, int ordinal);
}
