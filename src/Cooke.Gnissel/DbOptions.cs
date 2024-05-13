#region

using System.Collections.Concurrent;
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
    private readonly ConcurrentDictionary<Type, IDbConverter> _concreteConverters =
        new ConcurrentDictionary<Type, IDbConverter>(
            Converters
                .Select(x => (forType: GetConverterFor(x.GetType()), converter: x))
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
        var concreteConverter = _concreteConverters.GetValueOrDefault(typeof(T));
        if (concreteConverter != null)
        {
            return ((DbConverter<T>)concreteConverter).ToParameter(value, DbAdapter);
        }

        var converterFactory = _converterFactories.FirstOrDefault(x => x.CanCreateFor(typeof(T)));
        if (converterFactory != null)
        {
            var converter = converterFactory.Create(typeof(T));
            _concreteConverters.TryAdd(typeof(T), converter);
            return ((DbConverter<T>)converter).ToParameter(value, DbAdapter);
        }

        return DbAdapter.CreateParameter(value, dbType);
    }

    public RenderedSql RenderSql(Sql sql) => DbAdapter.RenderSql(sql, this);

    private static Type? GetConverterFor(Type? type)
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

public interface IDbConverter { }

public abstract class DbConverterFactory : IDbConverter
{
    public abstract bool CanCreateFor(Type type);

    public abstract IDbConverter Create(Type type);
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
    public override bool CanCreateFor(Type type) => type.IsEnum;

    public override IDbConverter Create(Type type)
    {
        return (IDbConverter?)
                Activator.CreateInstance(typeof(EnumStringDbConverter<>).MakeGenericType(type))
            ?? throw new InvalidOperationException();
    }
}
