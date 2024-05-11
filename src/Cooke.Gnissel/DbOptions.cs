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

    public IImmutableList<IDbConverter> Converters { get; init; } = Converters;

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
    public abstract DbParameter ToParameter(T value, IDbAdapter adapter);

    public abstract T FromReader(DbDataReader reader, int ordinal);
}

public interface IDbConverter;
