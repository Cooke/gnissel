using Cooke.Gnissel;
using Cooke.Gnissel.Npgsql;

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

void Test<T>()
{
    return dbContext.Query<T>($"");
}

public class User(
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

[DbContext]
public partial class MyDbContext;
