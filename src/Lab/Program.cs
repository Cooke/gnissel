using System.Collections.Immutable;
using System.Data.Common;
using System.Linq.Expressions;
using Cooke.Gnissel;
using Cooke.Gnissel.Services;
using Cooke.Gnissel.Typed;
using Gnissel.SourceGeneration;

var nameProvider = new DefaultDbNameProvider();
var mappers = new DbMappers(nameProvider)
{
    ReaderProvider = new CustomDbReaders(nameProvider) { UserReader = null! },
};

var dbContext = new MyDbContext(new DbOptions(null!, mappers));

await dbContext.Users.Select(x => new { x.Name, x.Address }).FirstOrDefault().ExecuteAsync();

public class MyDbContext(DbOptions options) : DbContext(options)
{
    public Table<User> Users { get; } = new(options);
}

public record UserId(string Value);

public record User(UserId Id, string Name, Address Address, UserType Type);

public record Address(string Street);

[DbMap(Technique = MappingTechnique.AsIs)]
public record GeoLocation(double Latitude, double Longitude);

public enum GeoType
{
    Point,
    Polygon,
}

public enum AnotherType
{
    One,
    Two,
}

[DbMap(Technique = MappingTechnique.AsString)]
public enum UserType
{
    Admin,
    User,
}

namespace Gnissel.SourceGeneration
{
    [DbMappers(EnumMappingTechnique = MappingTechnique.AsIs)]
    internal partial class DbMappers;
}

class CustomDbReaders : DbMappers.DbReaders
{
    public CustomDbReaders(IDbNameProvider nameProvider)
        : base(nameProvider)
    {
        AddressReader = new ObjectReader<Address?>(ReadAddress, CreateReadAddressDescriptors);
    }

    private ImmutableArray<ReadDescriptor> CreateReadAddressDescriptors()
    {
        throw new NotImplementedException();
    }

    private Address ReadAddress(DbDataReader datareader, OrdinalReader ordinalreader)
    {
        throw new NotImplementedException();
    }
}
