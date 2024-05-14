namespace Cooke.Gnissel;

public class DbConverterAttribute(Type converterType) : Attribute
{
    public Type ConverterType => converterType;
}
