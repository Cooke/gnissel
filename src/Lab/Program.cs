// See https://aka.ms/new-console-template for more information


using Cooke.Gnissel;
using Cooke.Gnissel.Services;
using Cooke.Gnissel.Typed;

var dbContext = new MyDbContext((IDbAdapter)null!);

[DbContext]
public partial class MyDbContext
{
    public Table<User> Users { get; } = new(dbOptions);
}

public class User(string Name, Address Address);

public struct Address;
