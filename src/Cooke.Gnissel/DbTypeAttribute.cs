namespace Cooke.Gnissel;

public class DbTypeAttribute(string dbType) : Attribute
{
    public string DbType { get; } = dbType;
}
