namespace Cooke.Gnissel;

public class DbTypeAttribute : Attribute
{
    public string DbType { get; init; }

    public DbTypeAttribute(string dbType)
    {
        DbType = dbType;
    }
}
