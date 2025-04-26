namespace Cooke.Gnissel;

public class DbNameAttribute(string? dbName) : Attribute
{
    public string? DbName { get; } = dbName;
}
