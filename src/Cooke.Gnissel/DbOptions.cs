#region

using System.Collections.Immutable;
using System.Data.Common;
using Cooke.Gnissel.Services;
using Cooke.Gnissel.Services.Implementations;
using Cooke.Gnissel.Typed.Services;

#endregion

namespace Cooke.Gnissel;

public record DbOptions(
    IDbAdapter DbAdapter,
    IObjectReaderProvider ObjectReaderProvider,
    IDbConnector DbConnector,
    IImmutableList<IDbConverter> Converters
)
{
    public DbOptions(IDbAdapter dbAdapter)
        : this(dbAdapter, new DefaultObjectReaderProvider(dbAdapter)) { }

    public DbOptions(IDbAdapter dbAdapter, IObjectReaderProvider objectReaderProvider)
        : this(dbAdapter, objectReaderProvider, dbAdapter.CreateConnector(), []) { }

    public ITypedSqlGenerator TypedSqlGenerator => DbAdapter.TypedSqlGenerator;

    public DbParameter CreateParameter<T>(T value, string? dbType) =>
        Converters.OfType<DbConverter<T>>().FirstOrDefault()?.ToParameter(value, DbAdapter)
        ?? DbAdapter.CreateParameter(value, dbType);

    public RenderedSql RenderSql(Sql sql) => DbAdapter.RenderSql(sql, this);
}

public class DbConverterAttribute(Type converterType) : Attribute
{
    public Type ConverterType => converterType;
}

public class DbConvertException : Exception
{
    public DbConvertException(Type fromType, Type toType) { }
}

public abstract class DbConverter<T> : IDbConverter
{
    public virtual bool CanConvert(Type type) => type == typeof(T);

    public abstract DbParameter ToParameter(T value, IDbAdapter adapter);

    public abstract T FromReader(DbDataReader reader, int ordinal);
}

public interface IDbConverter
{
    bool CanConvert(Type type);
}

public abstract class DbConverterFactory : IDbConverter
{
    public abstract bool CanConvert(Type type);

    public abstract IDbConverter CreateConverter(Type type);
}

public class EnumStringDbConverter<TEnum> : DbConverter<TEnum>
    where TEnum : struct, Enum
{
    public override DbParameter ToParameter(TEnum value, IDbAdapter adapter) =>
        adapter.CreateParameter(value.ToString());

    public override TEnum FromReader(DbDataReader reader, int ordinal) =>
        Enum.TryParse(reader.GetString(ordinal), false, out TEnum value)
            ? value
            : throw new DbConvertException(reader.GetFieldType(ordinal), typeof(TEnum));
}

public class EnumStringDbConverter : DbConverterFactory
{
    public override bool CanConvert(Type type) => type.IsEnum;

    public override IDbConverter CreateConverter(Type type)
    {
        return (IDbConverter?)
                Activator.CreateInstance(typeof(EnumStringDbConverter<>).MakeGenericType(type))
            ?? throw new InvalidOperationException();
    }
}
