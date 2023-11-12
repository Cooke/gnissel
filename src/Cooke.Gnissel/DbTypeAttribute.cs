namespace Cooke.Gnissel;

public class DbTypeAttribute : Attribute
{
    public string DbType { get; }

    public DbTypeAttribute(string dbType)
    {
        DbType = dbType;
    }
}
