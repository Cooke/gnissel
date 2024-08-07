﻿using System.Data.Common;

namespace Cooke.Gnissel.Converters;

public class EnumStringDbConverter : ConcreteDbConverterFactory
{
    public override bool CanCreateFor(Type type) => type.IsEnum;

    public override ConcreteDbConverter Create(Type type)
    {
        return (ConcreteDbConverter?)
                Activator.CreateInstance(typeof(EnumStringDbConverter<>).MakeGenericType(type))
            ?? throw new InvalidOperationException();
    }
}

public class EnumStringDbConverter<TEnum> : ConcreteDbConverter<TEnum>
    where TEnum : struct, Enum
{
    public override DbValue ToValue(TEnum value) => new DbValue<string>(value.ToString(), null);

    public override TEnum FromReader(DbDataReader reader, int ordinal) =>
        Enum.TryParse(reader.GetString(ordinal), false, out TEnum value)
            ? value
            : throw new DbConvertException(reader.GetFieldType(ordinal), typeof(TEnum));
}
