using Cooke.Gnissel;
using Cooke.Gnissel.Npgsql;
using Cooke.Gnissel.Queries;
using Cooke.Gnissel.Typed;

var adapter = new NpgsqlDbAdapter(null!);
var dbContext = new MyDbContext(adapter);
dbContext.Query<User>($"");
dbContext.Query<User?>($"");
dbContext.Query<(User, Device)>($"");
dbContext.Query<(User, User, User)>($"");
dbContext.Query<(User, User, User?)>($"");
dbContext.Query<Address?>($"");
dbContext.Query<(int, string, int)>($"");
dbContext.Query<(int, string?, int)>($"");
dbContext.Query<string>($"");
dbContext.Query<int>($"");
dbContext.Query<int?>($"");

dbContext.Query<DateTime>($"");
dbContext.Query<DateTime?>($"");
dbContext.Query<TimeSpan>($"");
dbContext.Query<TimeSpan?>($"");

dbContext.Query<(TimeSpan?, DateTime)>($"");
dbContext.Query<(TimeSpan, DateTime)>($"");

Query<T> Test<T>()
{
    return dbContext.Query<T>($"");
}

var users = dbContext.Users.Select(x => new { x.Name, x.Age });

public record User(
    string Name,
    int Age,
    int? Size,
    Role role,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    TimeSpan PlayTime,
    TimeSpan? IdleTime,
    Address address
);

public class Device(string Name, string? Model);

public class Address(string Street, string City, string? ZipCode);

public enum Role
{
    Admin,
    User,
}

[DbContext(EnumMappingTechnique = EnumMappingTechnique.Direct)]
public partial class MyDbContext
{
    public Table<User> Users { get; } = new(options);
}
