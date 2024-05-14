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

public record DbOptions(
    IDbAdapter DbAdapter,
    IObjectReaderProvider ObjectReaderProvider,
    IDbConnector DbConnector,
    IImmutableList<IDbConverter> Converters
)
{
    private readonly ConcurrentDictionary<Type, IDbConverter> _concreteConverters =
        new ConcurrentDictionary<Type, IDbConverter>(
            Converters
                .Select(x => (forType: GetTypeToConvertFor(x.GetType()), converter: x))
                .Where(x => x.forType != null)
                .Select(x => new KeyValuePair<Type, IDbConverter>(x.forType!, x.converter))
        );

    private readonly ImmutableList<DbConverterFactory> _converterFactories = Converters
        .OfType<DbConverterFactory>()
        .ToImmutableList();

    public DbOptions(IDbAdapter dbAdapter)
        : this(dbAdapter, new DefaultObjectReaderProvider(dbAdapter)) { }

    public DbOptions(IDbAdapter dbAdapter, IObjectReaderProvider objectReaderProvider)
        : this(dbAdapter, objectReaderProvider, dbAdapter.CreateConnector(), []) { }

    public ITypedSqlGenerator TypedSqlGenerator => DbAdapter.TypedSqlGenerator;

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
        IDbConverter? converter;
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
                (IDbConverter?)Activator.CreateInstance(converterAttribute.ConverterType)
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
