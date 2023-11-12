namespace Cooke.Gnissel;

public class DbNameAttribute : Attribute
{
    public string DbName { get; }

    public DbNameAttribute(string dbName)
    {
        DbName = dbName;
    }
}
