// See https://aka.ms/new-console-template for more information


using System.Data.Common;
using Cooke.Gnissel;
using Cooke.Gnissel.Services;
using Cooke.Gnissel.SourceGeneration;
using Cooke.Gnissel.Typed;

var dbContext = new MyDbContext(new DbOptions(null!, ObjectReaders.Descriptors));

await dbContext.Users.Select(x => new { x.Name, x.Address }).FirstOrDefault().ExecuteAsync();

public class MyDbContext(DbOptions options) : DbContext(options)
{
    public Table<User> Users { get; } = new(options);
}

public record User(string Name, Address Address);

public struct Address;
