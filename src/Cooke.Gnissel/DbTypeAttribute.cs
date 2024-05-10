namespace Cooke.Gnissel;

public class DbTypeAttribute(string dbType) : Attribute
{
    public string DbType { get; } = dbType;
}


public class DbMapping(DbMappingType type) : Attribute
{
    public DbMappingType Type { get; } = type;
}

public enum DbMappingType
{
    WrappedPrimitive
}
