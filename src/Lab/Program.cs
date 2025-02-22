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

[ObjectReaders]
public static partial class ObjectReaders
{
    public static IObjectReaderDescriptor Create()
    {
        var metadata = new NextOrdinalObjectReaderMetadata();
        var readFactory = (ObjectReaderCreateContext context) =>
            (DbDataReader dbReader, OrdinalReader ordinalReader) =>
                new { Name = dbReader.GetString(ordinalReader.Read()) };

        return CreateObjectReader(readFactory, metadata);
    }
}

public record User(string Name, Address Address);

public struct Address;
