using System.Data.Common;
using Cooke.Gnissel.Services;

namespace Cooke.Gnissel;

public abstract class DbConverter;

public abstract class ConcreteDbConverter : DbConverter
{
    public abstract DbValue ToDbValue(object value);

    public virtual bool IsNull(DbDataReader reader, int ordinal) => reader.IsDBNull(ordinal);
}

public abstract class DbConverter<T> : ConcreteDbConverter
{
    public abstract DbParameter ToParameter(T value, IDbAdapter adapter);

    public abstract T FromReader(DbDataReader reader, int ordinal);

    public override DbValue ToDbValue(object value) => new TypedDbValue<T>((T)value);
}
