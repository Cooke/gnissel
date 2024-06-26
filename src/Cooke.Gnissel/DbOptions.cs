#region

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Data.Common;
using System.Reflection;
using Cooke.Gnissel.Services;
using Cooke.Gnissel.Services.Implementations;
using Cooke.Gnissel.Typed.Services;

#endregion

namespace Cooke.Gnissel;

public class DbOptions(
    IDbAdapter adapter,
    IObjectReaderProvider objectReaderProvider,
    IDbConnector connector,
    IImmutableList<DbConverter> converters
)
{
    private readonly ConcurrentDictionary<Type, DbConverter> _concreteConverters =
        new(
            converters
                .Select(x => (forType: GetTypeToConvertFor(x.GetType()), converter: x))
                .Where(x => x.forType != null)
                .Select(x => new KeyValuePair<Type, DbConverter>(x.forType!, x.converter))
        );

    private readonly ImmutableList<DbConverterFactory> _converterFactories = converters
        .OfType<DbConverterFactory>()
        .ToImmutableList();

    public DbOptions(IDbAdapter adapter)
        : this(adapter, new DefaultObjectReaderProvider(adapter)) { }

    public DbOptions(IDbAdapter adapter, IObjectReaderProvider objectReaderProvider)
        : this(adapter, objectReaderProvider, adapter.CreateConnector(), []) { }

    public DbOptions(IDbAdapter adapter, IImmutableList<DbConverter> converters)
        : this(
            adapter,
            new DefaultObjectReaderProvider(adapter),
            adapter.CreateConnector(),
            converters
        ) { }

    public DbOptions(IDbAdapter adapter, IDbConnector connector)
        : this(adapter, new DefaultObjectReaderProvider(adapter), connector, []) { }

    public ITypedSqlGenerator TypedSqlGenerator => DbAdapter.TypedSqlGenerator;

    public IDbAdapter DbAdapter => adapter;

    public IObjectReaderProvider ObjectReaderProvider => objectReaderProvider;

    public IDbConnector DbConnector => connector;

    public DbParameter CreateParameter<T>(T value, string? dbType)
    {
        var converter = GetConverter<T>();
        return converter != null
            ? converter.ToParameter(value, DbAdapter)
            : DbAdapter.CreateParameter(value, dbType);
    }

    public RenderedSql RenderSql(Sql sql) => DbAdapter.RenderSql(sql, this);

    private static Type? GetTypeToConvertFor(Type? type)
    {
        while (true)
        {
            if (type is null)
            {
                return null;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(DbConverter<>))
            {
                return type.GetGenericArguments().Single();
            }

            type = type.BaseType;
        }
    }

    public DbConverter<T>? GetConverter<T>()
    {
        return (DbConverter<T>?)GetConverter(typeof(T));
    }

    public object? GetConverter(Type type)
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

    private object GetConverterFromAttribute(Type type, DbConverterAttribute converterAttribute)
    {
        DbConverter? converter;
        if (converterAttribute.ConverterType.IsAssignableTo(typeof(DbConverterFactory)))
        {
            var factory =
                (DbConverterFactory?)Activator.CreateInstance(converterAttribute.ConverterType)
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
                typeof(DbConverter<>).MakeGenericType(type)
            )
        )
        {
            converter =
                (DbConverter?)Activator.CreateInstance(converterAttribute.ConverterType)
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

    public DbOptions WithDbConnector(IDbConnector connector) =>
        new(DbAdapter, ObjectReaderProvider, connector, converters);
}
