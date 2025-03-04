using Cooke.Gnissel;
using Cooke.Gnissel.Typed;

var dbContext = new MyDbContext(null!, ObjectReaders.Descriptors);

await dbContext.Users.Select(x => new { x.Name, x.Address }).FirstOrDefault().ExecuteAsync();

public class MyDbContext(DbOptions options) : DbContext(options)
{
    public Table<User> Users { get; } = new(options);
}

public record UserId(string Value);

public record User(UserId Id, string Name, Address Address, UserType Type);

public struct Address;

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

[DbMap(EnumTechnique = EnumMappingTechnique.String)]
public enum UserType
{
    Admin,
    User,
}

[DbMappers(
    OptIn = false,
    MappingTechnique = MappingTechnique.Default,
    EnumMappingTechnique = EnumMappingTechnique.String
)]
[DbMap(typeof(AnotherType), MappingTechnique = MappingTechnique.AsIs)]
[DbMap(typeof(UserType))]
internal partial class ObjectReaders { }
