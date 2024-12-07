#region

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Data.Common;
using Cooke.Gnissel.Services;
using Cooke.Gnissel.Services.Implementations;

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

    public DbOptions(IDbAdapter adapter)
        : this(adapter, new DefaultObjectReaderProvider(adapter)) { }

    public DbOptions(IDbAdapter adapter, IObjectReaderProvider objectReaderProvider)
        : this(adapter, objectReaderProvider, adapter.CreateConnector(), []) { }

    public DbOptions(IDbAdapter adapter, IDbConnector connector)
        : this(adapter, new DefaultObjectReaderProvider(adapter), connector, []) { }

    public DbOptions(IDbAdapter adapter, IImmutableList<DbConverter> converters)
        : this(
            adapter,
            new DefaultObjectReaderProvider(adapter),
            adapter.CreateConnector(),
            converters
        ) { }

    public DbOptions(
        IDbAdapter adapter,
        IObjectReaderProvider objectReaderProvider,
        IDbConnector connector,
        IImmutableList<DbConverter> converters
    )
    {
        _adapter = adapter;
        _objectReaderProvider = objectReaderProvider;
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
        IDbConnector connector,
        IImmutableList<DbConverter> converters
    )
    {
        _concreteConverters = concreteConverters;
        _converterFactories = converterFactories;
        _adapter = adapter;
        _objectReaderProvider = objectReaderProvider;
        _converters = converters;
        _connector = connector;
    }

    public DbOptions WithConnector(IDbConnector newConnector) =>
        new(
            _concreteConverters,
            _converterFactories,
            DbAdapter,
            _objectReaderProvider,
            newConnector,
            _converters
        );

    public IDbAdapter DbAdapter => _adapter;

    public IDbConnector DbConnector => _connector;

    public DbParameter CreateParameter<T>(T value, string? dbType)
    {
        var converter = GetConverter<T>();
        return converter != null
            ? converter.ToValue(value).CreateParameter(DbAdapter)
            : DbAdapter.CreateParameter(value, dbType);
    }

    public RenderedSql RenderSql(Sql sql) => DbAdapter.RenderSql(sql, this);

    public ObjectReader<T> GetReader<T>() => _objectReaderProvider.Get<T>(this);

    public bool IsDbMapped(Type type) => GetConverter(type) != null || DbAdapter.IsDbMapped(type);
}
