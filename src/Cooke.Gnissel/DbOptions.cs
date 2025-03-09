#region

using System.Data.Common;
using Cooke.Gnissel.Services;
using Cooke.Gnissel.SourceGeneration;

#endregion

namespace Cooke.Gnissel;

public partial class DbOptions(
    IDbAdapter adapter,
    IObjectReaderProvider objectReaderProvider,
    IObjectWriterProvider objectWriterProvider,
    IDbConnector connector
)
{
    public DbOptions(IDbAdapter adapter, IReadOnlyCollection<IObjectMapperDescriptor> descriptors)
        : this(
            adapter,
            new ObjectReaderProviderBuilder(descriptors.OfType<IObjectReaderDescriptor>()).Build(
                adapter
            ),
            new ObjectWriterProviderBuilder(descriptors.OfType<IObjectWriterDescriptor>()).Build(
                adapter
            )
        ) { }

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

    public bool IsDbMapped(Type type) => DbAdapter.IsDbMapped(type);
}
