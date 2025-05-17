#region

using System.Data.Common;
using Cooke.Gnissel.Services;

#endregion

namespace Cooke.Gnissel;

public class DbOptions(IDbAdapter adapter, IMapperProvider mapperProvider, IDbConnector connector)
{
    public DbOptions(IDbAdapter adapter, IMapperProvider mapperProvider)
        : this(adapter, mapperProvider, adapter.CreateConnector()) { }

    public DbOptions WithConnector(IDbConnector newConnector) =>
        new(DbAdapter, mapperProvider, newConnector);

    public IDbAdapter DbAdapter => adapter;

    public IDbConnector DbConnector => connector;

    public IMapperProvider MapperProvider => mapperProvider;

    public DbParameter CreateParameter<T>(T value, string? dbType) =>
        adapter.CreateParameter(value, dbType);

    public RenderedSql RenderSql(Sql sql) => DbAdapter.RenderSql(sql, this);

    public ObjectReader<T> GetReader<T>() => mapperProvider.ReaderProvider.Get<T>();

    public IObjectReader GetReader(Type type) => mapperProvider.ReaderProvider.Get(type);

    public ObjectWriter<T> GetWriter<T>() => mapperProvider.WriterProvider.Get<T>();

    public IObjectWriter GetWriter(Type type) => mapperProvider.WriterProvider.Get(type);
}
