using System.Reflection;

namespace Cooke.Gnissel;

public partial class DbOptions
{
    private ConcreteDbConverter<T>? GetConverter<T>()
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
