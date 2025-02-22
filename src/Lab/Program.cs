// See https://aka.ms/new-console-template for more information


using Cooke.Gnissel;
using Cooke.Gnissel.Services;
using Cooke.Gnissel.SourceGeneration;
using Cooke.Gnissel.Typed;

var dbContext = new DbContext(new DbOptions(null!, ObjectReaders.Descriptors));

public class MyDbContext(DbOptions options) : DbContext(options)
{
    public Table<User> Users { get; } = new(options);
}

[ObjectReaders]
public static partial class ObjectReaders;

public class User(string Name, Address Address);

public struct Address;
