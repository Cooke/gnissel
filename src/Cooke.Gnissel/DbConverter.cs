using System.Data.Common;
using Cooke.Gnissel.Services;

namespace Cooke.Gnissel;

public abstract class DbConverter;

public abstract class ConcreteDbConverterFactory : DbConverter
{
    public abstract bool CanCreateFor(Type type);

    public abstract ConcreteDbConverter Create(Type type);
}

public abstract class ConcreteDbConverter : DbConverter
{
    public abstract DbValue ToDbValue(object value);

    public virtual bool IsNull(DbDataReader reader, int ordinal) => reader.IsDBNull(ordinal);
}

public abstract class ConcreteDbConverter<T> : ConcreteDbConverter
{
    public abstract DbParameter ToParameter(T value, IDbAdapter adapter);

    public abstract T FromReader(DbDataReader reader, int ordinal);

    public override DbValue ToDbValue(object value) => new TypedDbValue<T>((T)value);
}

public class DbConverterAttribute(Type converterType) : Attribute
{
    public Type ConverterType => converterType;
}

public class DbConvertException : Exception
{
    public DbConvertException(Type fromType, Type toType) { }
}
