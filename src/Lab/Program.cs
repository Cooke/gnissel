using Cooke.Gnissel;
using Cooke.Gnissel.Typed;
using Gnissel.SourceGeneration;

var mappers = new DbMappers
{
    Readers = new DbMappers.DbReaders { UserReader = new ObjectReader<User?>(null!, null!) },
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
    internal partial class DbMappers { }
}
