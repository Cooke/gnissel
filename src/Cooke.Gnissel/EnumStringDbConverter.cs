using System.Data.Common;
using Cooke.Gnissel.Services;

namespace Cooke.Gnissel;

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
