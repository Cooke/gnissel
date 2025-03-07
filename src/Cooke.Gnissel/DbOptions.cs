#region

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Data.Common;
using Cooke.Gnissel.Services;
using Cooke.Gnissel.SourceGeneration;

#endregion

namespace Cooke.Gnissel;

public partial class DbOptions
{
    private readonly ConcurrentDictionary<Type, ConcreteDbConverter> _concreteConverters;
    private readonly ImmutableList<ConcreteDbConverterFactory> _converterFactories;
    private readonly IDbAdapter _adapter;
    private readonly IObjectReaderProvider _objectReaderProvider;
    private readonly IDbConnector _connector;
    private readonly IImmutableList<DbConverter> _converters;
    private readonly IObjectWriterProvider _objectWriterProvider;

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
        : this(adapter, objectReaderProvider, objectWriterProvider, adapter.CreateConnector(), [])
    { }

    public DbOptions(
        IDbAdapter adapter,
        IObjectReaderProvider objectReaderProvider,
        IObjectWriterProvider objectWriterProvider,
        IDbConnector connector
    )
        : this(adapter, objectReaderProvider, objectWriterProvider, connector, []) { }

    public DbOptions(
        IDbAdapter adapter,
        IObjectReaderProvider objectReaderProvider,
        IObjectWriterProvider objectWriterProvider,
        IImmutableList<DbConverter> converters
    )
        : this(
            adapter,
            objectReaderProvider,
            objectWriterProvider,
            adapter.CreateConnector(),
            converters
        ) { }

    public DbOptions(
        IDbAdapter adapter,
        IObjectReaderProvider objectReaderProvider,
        IObjectWriterProvider objectWriterProvider,
        IDbConnector connector,
        IImmutableList<DbConverter> converters
    )
    {
        _adapter = adapter;
        _objectReaderProvider = objectReaderProvider;
        _objectWriterProvider = objectWriterProvider;
        _connector = connector;
        _converters = converters;
        _concreteConverters = new(
            converters
                .OfType<ConcreteDbConverter>()
                .Select(x => (forType: GetTypeToConvertFor(x.GetType()), converter: x))
                .Where(x => x.forType != null)
                .Select(x => new KeyValuePair<Type, ConcreteDbConverter>(x.forType!, x.converter))
        );
        _converterFactories = converters.OfType<ConcreteDbConverterFactory>().ToImmutableList();
    }

    private DbOptions(
        ConcurrentDictionary<Type, ConcreteDbConverter> concreteConverters,
        ImmutableList<ConcreteDbConverterFactory> converterFactories,
        IDbAdapter adapter,
        IObjectReaderProvider objectReaderProvider,
        IObjectWriterProvider objectWriterProvider,
        IDbConnector connector,
        IImmutableList<DbConverter> converters
    )
    {
        _concreteConverters = concreteConverters;
        _converterFactories = converterFactories;
        _adapter = adapter;
        _objectReaderProvider = objectReaderProvider;
        _objectWriterProvider = objectWriterProvider;
        _converters = converters;
        _connector = connector;
    }

    public DbOptions WithConnector(IDbConnector newConnector) =>
        new(
            _concreteConverters,
            _converterFactories,
            DbAdapter,
            _objectReaderProvider,
            _objectWriterProvider,
            newConnector,
            _converters
        );

    public IDbAdapter DbAdapter => _adapter;

    public IDbConnector DbConnector => _connector;

    public DbParameter CreateParameter<T>(T value, string? dbType) =>
        _adapter.CreateParameter(value, dbType);

    public RenderedSql RenderSql(Sql sql) => DbAdapter.RenderSql(sql, this);

    public ObjectReader<T> GetReader<T>() => _objectReaderProvider.Get<T>();

    public bool IsDbMapped(Type type) => GetConverter(type) != null || DbAdapter.IsDbMapped(type);
}
