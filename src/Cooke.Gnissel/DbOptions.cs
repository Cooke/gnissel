#region

using System.Data.Common;
using Cooke.Gnissel.Services;

#endregion

namespace Cooke.Gnissel;

public partial class DbOptions(
    IDbAdapter adapter,
    IObjectReaderProvider objectReaderProvider,
    IObjectWriterProvider objectWriterProvider,
    IDbConnector connector
)
{
    public DbOptions(IDbAdapter adapter, IMapperProvider mapperProvider)
        : this(adapter, mapperProvider.ReaderProvider, mapperProvider.WriterProvider) { }

    public DbOptions(
        IDbAdapter adapter,
        IObjectReaderProvider objectReaderProvider,
        IObjectWriterProvider objectWriterProvider
    )
        : this(adapter, objectReaderProvider, objectWriterProvider, adapter.CreateConnector()) { }

    public DbOptions WithConnector(IDbConnector newConnector) =>
        new(DbAdapter, objectReaderProvider, objectWriterProvider, newConnector);

    public IDbAdapter DbAdapter => adapter;

    public IDbConnector DbConnector => connector;

    public DbParameter CreateParameter<T>(T value, string? dbType) =>
        adapter.CreateParameter(value, dbType);

    public RenderedSql RenderSql(Sql sql) => DbAdapter.RenderSql(sql, this);

    public ObjectReader<T> GetReader<T>() => objectReaderProvider.Get<T>();

    public ObjectWriter<T> GetWriter<T>() => objectWriterProvider.Get<T>();

    public IObjectWriter GetWriter(Type type) => objectWriterProvider.Get(type);

    public bool IsDbMapped(Type type) => DbAdapter.IsDbMapped(type);
}
