using Cooke.Gnissel.AsyncEnumerable;
using Cooke.Gnissel.Npgsql;
using Cooke.Gnissel.Typed.Test.Fixtures;
using Xunit.Abstractions;

namespace Cooke.Gnissel.Typed.Test;

[Collection("Database collection")]
public partial class EnumTests : IDisposable
{
    private readonly TestDbContext db;

    public EnumTests(DatabaseFixture databaseFixture, ITestOutputHelper testOutputHelper)
    {
        databaseFixture.SetOutputHelper(testOutputHelper);
        db = new TestDbContext(
            new(new NpgsqlDbAdapter(databaseFixture.DataSourceBuilder.Build()), new DbMappers())
        );
        db.NonQuery(
                $"""
                    create table users
                    (
                        id   integer primary key,
                        name text,
                        age  integer,
                        role text
                    )
                """
            )
            .GetAwaiter()
            .GetResult();
    }

    public void Dispose()
    {
        db.NonQuery($"DROP TABLE users").GetAwaiter().GetResult();
    }

    [Fact]
    public async Task Insert()
    {
        var bob = new User(1, "Bob", 25, Role.Admin);
        await db.Users.Insert(bob);

        var users = await db.Query<User>($"SELECT * FROM users").ToArrayAsync();

        Assert.Equal([bob], users);
    }

    [Fact]
    public async Task Update()
    {
        var bob = new User(1, "Bob", 25, Role.Admin);
        await db.Users.Insert(bob);

        await db.Users.Set(x => x.Role, Role.User).WithoutWhere();

        var users = await db.Query<User>($"SELECT * FROM users").ToArrayAsync();
        Assert.Equal([bob with { Role = Role.User }], users);
    }

    [Fact]
    public async Task CompareEnumValues()
    {
        var bob = new User(1, "Bob", 30, Role.User);
        var sara = new User(2, "Sara", 20, Role.Admin);
        await db.Users.Insert(bob, sara);

        var users = await db.Users.Where(x => x.Role == Role.Admin).ToArrayAsync();
        Assert.Equal([sara], users);
    }

    private class TestDbContext(DbOptions options) : DbContext(options)
    {
        public Table<User> Users { get; } = new(options);
    }

    private record User(int Id, string Name, int Age, Role Role);

    private enum Role
    {
        Admin,
        User,
    }

    [DbMappers(EnumMappingTechnique = MappingTechnique.AsString)]
    private partial class DbMappers;
}
