namespace Cooke.Gnissel;

public class DbNameAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}
