var dbContext = new DbContext();
dbContext.Query<User>("");
dbContext.Query<User?>("");
dbContext.Query<(User, Device)>("");
dbContext.Query<(User, User, User?)>("");
dbContext.Query<(int, string, int)>("");
dbContext.Query<(int, string?, int)>("");
dbContext.Query<string>("");
dbContext.Query<int>("");
dbContext.Query<int?>("");
dbContext.Query<DateTime>("");
dbContext.Query<DateTime?>("");
dbContext.Query<TimeSpan>("");
dbContext.Query<TimeSpan?>("");
dbContext.Query<(TimeSpan?, DateTime)>("");

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

public class DbContext
{
    public void Query<TOut>(string sql) { }
}
