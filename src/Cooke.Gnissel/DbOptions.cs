#region

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Data.Common;
using System.Reflection;
using Cooke.Gnissel.Services;
using Cooke.Gnissel.Services.Implementations;

#endregion

namespace Cooke.Gnissel;

public class DbOptions(
    IDbAdapter adapter,
    IObjectReaderFactory objectReaderFactory,
    IDbConnector connector,
    IImmutableList<DbConverter> converters
)
{
    private readonly ConcurrentDictionary<Type, object> _readers = new();

    private readonly ConcurrentDictionary<Type, ConcreteDbConverter> _concreteConverters =
        new(
            converters
                .OfType<ConcreteDbConverter>()
                .Select(x => (forType: GetTypeToConvertFor(x.GetType()), converter: x))
                .Where(x => x.forType != null)
                .Select(x => new KeyValuePair<Type, ConcreteDbConverter>(x.forType!, x.converter))
        );

    private readonly ImmutableList<ConcreteDbConverterFactory> _converterFactories = converters
        .OfType<ConcreteDbConverterFactory>()
        .ToImmutableList();

    public DbOptions(IDbAdapter adapter)
        : this(adapter, new DefaultObjectReaderFactory(adapter)) { }

    public DbOptions(IDbAdapter adapter, IObjectReaderFactory objectReaderFactory)
        : this(adapter, objectReaderFactory, adapter.CreateConnector(), []) { }

    public DbOptions(IDbAdapter adapter, IImmutableList<DbConverter> converters)
        : this(
            adapter,
            new DefaultObjectReaderFactory(adapter),
            adapter.CreateConnector(),
            converters
        ) { }

    public DbOptions(IDbAdapter adapter, IDbConnector connector)
        : this(adapter, new DefaultObjectReaderFactory(adapter), connector, []) { }

    public IDbAdapter DbAdapter => adapter;

    public IObjectReaderFactory ObjectReaderFactory => objectReaderFactory;

    public IDbConnector DbConnector => connector;

    public DbOptions WithDbConnector(IDbConnector newConnector) =>
        new(DbAdapter, ObjectReaderFactory, newConnector, converters);

    public DbParameter CreateParameter<T>(T value, string? dbType)
    {
        var converter = GetConverter<T>();
        return converter != null
            ? converter.ToValue(value).CreateParameter(DbAdapter)
            : DbAdapter.CreateParameter(value, dbType);
    }

    public RenderedSql RenderSql(Sql sql) => DbAdapter.RenderSql(sql, this);

    public ObjectReader<T> GetReader<T>() =>
        (ObjectReader<T>)_readers.GetOrAdd(typeof(T), _ => ObjectReaderFactory.Create<T>(this));

    public ConcreteDbConverter<T>? GetConverter<T>()
    {
        return (ConcreteDbConverter<T>?)GetConverter(typeof(T));
    }

    public ConcreteDbConverter? GetConverter(Type type)
    {
        if (_concreteConverters.TryGetValue(type, out var converter))
        {
            return converter;
        }

        var converterAttribute = type.GetCustomAttribute<DbConverterAttribute>();
        if (converterAttribute != null)
        {
            return GetConverterFromAttribute(type, converterAttribute);
        }

        var factory = _converterFactories.FirstOrDefault(x => x.CanCreateFor(type));
        if (factory != null)
        {
            var newConverter = factory.Create(type);
            _concreteConverters.TryAdd(type, newConverter);
            return newConverter;
        }

        return null;
    }

    private static Type? GetTypeToConvertFor(Type? type)
    {
        while (true)
        {
            if (type is null)
            {
                return null;
            }

            if (
                type.IsGenericType
                && type.GetGenericTypeDefinition() == typeof(ConcreteDbConverter<>)
            )
            {
                return type.GetGenericArguments().Single();
            }

            type = type.BaseType;
        }
    }

    private ConcreteDbConverter GetConverterFromAttribute(
        Type type,
        DbConverterAttribute converterAttribute
    )
    {
        ConcreteDbConverter? converter;
        if (converterAttribute.ConverterType.IsAssignableTo(typeof(ConcreteDbConverterFactory)))
        {
            var factory =
                (ConcreteDbConverterFactory?)
                    Activator.CreateInstance(converterAttribute.ConverterType)
                ?? throw new InvalidOperationException(
                    $"Could not create an instance of converter factory of type {converterAttribute.ConverterType}"
                );
            if (!factory.CanCreateFor(type))
            {
                throw new InvalidOperationException(
                    $"Factory of type {factory.GetType()} cannot create converter for type {type}"
                );
            }

            converter = factory.Create(type);
            _concreteConverters.TryAdd(type, converter);
            return converter;
        }

        if (
            converterAttribute.ConverterType.IsAssignableTo(
                typeof(ConcreteDbConverter<>).MakeGenericType(type)
            )
        )
        {
            converter =
                (ConcreteDbConverter?)Activator.CreateInstance(converterAttribute.ConverterType)
                ?? throw new InvalidOperationException(
                    $"Could not create an instance of converter of type {converterAttribute.ConverterType}"
                );
            _concreteConverters.TryAdd(type, converter);
            return converter;
        }

        throw new InvalidOperationException(
            $"Converter of type {converterAttribute.ConverterType} is not a valid converter for type {type}"
        );
    }
}
