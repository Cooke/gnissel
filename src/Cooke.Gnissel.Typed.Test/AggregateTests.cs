using Cooke.Gnissel.Npgsql;
using Cooke.Gnissel.Typed.Test.Fixtures;
using Cooke.Gnissel.Utils;
using Npgsql;
using Xunit.Abstractions;

namespace Cooke.Gnissel.Typed.Test;

[Collection("Database collection")]
public class AggregateTests : IDisposable
{
    private readonly TestDbContext db;
        
    public AggregateTests(DatabaseFixture databaseFixture, ITestOutputHelper testOutputHelper) 
    {
        databaseFixture.SetOutputHelper(testOutputHelper);
        db = new TestDbContext(new(
            new NpgsqlDbAdapter(databaseFixture.DataSourceBuilder.Build())
        ));
        db.NonQuery(
                $"""
                    create table users
                    (
                        id   integer primary key,
                        name text,
                        age  integer
                    );
                """
            ).GetAwaiter().GetResult();
    }
    
    public void Dispose()
    {
        db.NonQuery($"DROP TABLE users").GetAwaiter().GetResult();
    }
    
    
    [Fact]
    public async Task Count()
    {
        var bob = new User(1, "Bob", 30);
        var sara = new User(2, "Sara", 20);
        var alice = new User(3, "Alice", 20);
        await db.Users.Insert(bob, sara, alice);
        
        var numUsersByAge = await db.Users.GroupBy(x => x.Age).Select(x => new { x.Age, Count = Db.Count(x.Id) }).ToArrayAsync();
        
        Assert.Equal(
            [new { Age = 30, Count = 1 }, new { Age = 20 , Count = 2}],
            numUsersByAge
        );
    }
    
    [Fact]
    public async Task Sum()
    {
        var bob = new User(1, "Bob", 30);
        var sara = new User(2, "Sara", 20);
        var alice = new User(3, "Alice", 20);
        await db.Users.Insert(bob, sara, alice);
        
        var numUsersByAge = await db.Users.Select(x => Db.Sum(x.Age) ).First();
        
        Assert.Equal(
            70,
            numUsersByAge
        );
    }
    
    [Fact]
    public async Task Avg()
    {
        var bob = new User(1, "Bob", 30);
        var sara = new User(2, "Sara", 20);
        var alice = new User(3, "Alice", 20);
        await db.Users.Insert(bob, sara, alice);
        
        var numUsersByAge = await db.Users.Select(x => Db.Avg(x.Age) ).First();
        
        Assert.Equal(
            23,
            numUsersByAge
        );
    }
    
    [Fact]
    public async Task Max()
    {
        var bob = new User(1, "Bob", 30);
        var sara = new User(2, "Sara", 20);
        var alice = new User(3, "Alice", 20);
        await db.Users.Insert(bob, sara, alice);
        
        var numUsersByAge = await db.Users.Select(x => Db.Max(x.Age) ).First();
        
        Assert.Equal(
            30,
            numUsersByAge
        );
    }
    
    [Fact]
    public async Task Min()
    {
        var bob = new User(1, "Bob", 30);
        var sara = new User(2, "Sara", 20);
        var alice = new User(3, "Alice", 20);
        await db.Users.Insert(bob, sara, alice);
        
        var numUsersByAge = await db.Users.Select(x => Db.Min(x.Age)).First();
        
        Assert.Equal(
            20,
            numUsersByAge
        );
    }
    
    private class TestDbContext(DbOptions options) : DbContext(options)
    {
        public Table<User> Users { get; } = new(options);
    }
    
    private record User(
        int Id,
        string Name,
        int Age
    );
}