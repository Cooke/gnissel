namespace Cooke.Gnissel;

public class DbNameAttribute : Attribute
{
    public string DbName { get; init; }

    public DbNameAttribute(string dbName)
    {
        DbName = dbName;
    }
}
