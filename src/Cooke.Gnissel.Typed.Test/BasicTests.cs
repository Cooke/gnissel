using Cooke.Gnissel.Npgsql;
using Cooke.Gnissel.Typed.Test.Fixtures;
using Cooke.Gnissel.Utils;
using Xunit.Abstractions;

namespace Cooke.Gnissel.Typed.Test;

[Collection("Database collection")]
public class BasicTests : IDisposable
{
    private readonly TestDbContext db;
        
    public BasicTests(DatabaseFixture databaseFixture, ITestOutputHelper testOutputHelper) 
    {
        databaseFixture.SetOutputHelper(testOutputHelper);
        db = new TestDbContext(new DbOptionsTyped(new NpgsqlDbAdapter(databaseFixture.DataSourceBuilder.Build())));
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
    public async Task Insert( )
    
    {
        
        
        await db.Users.Insert(new User(1, "Bob", 25));
        await db.Users.Insert(new User(2, "Sara", 25));
        
        var users = await db.Query<User>($"SELECT * FROM users").ToArrayAsync();
        
        Assert.Equal(
            new[] { (1, "Bob"), (2, "Sara") },
            users.Select(x => (x.Id, x.Name))
        );
    }
    
    [Fact]
    public async Task InsertMany()
    {
        await db.Users.Insert(new User(1, "Bob", 25), new User(2, "Sara", 25));
        
        var users = await db.Query<User>($"SELECT * FROM users").ToArrayAsync();
        
        Assert.Equal(
            new[] { (1, "Bob"), (2, "Sara") },
            users.Select(x => (x.Id, x.Name))
        );
    }
    
    [Fact]
    public async Task InsertBatch()
    {
        await db.Batch(
            db.Users.Insert(new User(1, "Bob", 25)),
            db.Users.Insert(new User(2, "Sara", 25)));
        
        var users = await db.Query<User>($"SELECT * FROM users").ToArrayAsync();
        
        Assert.Equal(
            new[] { (1, "Bob"), (2, "Sara") },
            users.Select(x => (x.Id, x.Name))
        );
    }
    
    [Fact]
    public async Task QueryAll()
    {
        await db.Users.Insert(new User(1, "Bob", 25));
        await db.Users.Insert(new User(2, "Sara", 25));

        var users = await db.Users.ToArrayAsync();
        
        Assert.Equal(
            [new User(1, "Bob", 25), new User(2, "Sara", 25)],
            users
        );
    }
    
    [Fact]
    public async Task SelectOneColumn()
    {
        await db.Users.Insert(new User(1, "Bob", 25));
        await db.Users.Insert(new User(2, "Sara", 25));

        var names = await db.Users.Select(x => x.Name).ToArrayAsync();
        
        Assert.Equal(
            ["Bob", "Sara"],
            names
        );
    }
    
    [Fact]
    public async Task SelectSeveralColumns()
    {
        await db.Users.Insert(new User(1, "Bob", 25));
        await db.Users.Insert(new User(2, "Sara", 25));

        var users = await db.Users.Select(x => new { x.Name, x.Age }).ToArrayAsync();
        
        Assert.Equal(
            [new { Name =  "Bob", Age = 25 }, new { Name = "Sara", Age = 25}],
            users
        );
    }
    
    [Fact]
    public async Task SelectClass()
    {
        await db.Users.Insert(new User(1, "Bob", 25));
        await db.Users.Insert(new User(2, "Sara", 25));

        var users = await db.Users.Select(x => new User(x.Id, x.Name, x.Age)).ToArrayAsync();
        
        Assert.Equal(
            [new User(1, "Bob", 25), new User(2, "Sara", 25)],
            users
        );
    }
    
    [Fact]
    public async Task WhereConstant()
    {
        await db.Users.Insert(new User(1, "Bob", 25));
        await db.Users.Insert(new User(2, "Sara", 25));
        
        var users = await db.Users.Where(x => x.Name == "Bob").ToArrayAsync();
        
        Assert.Equal([new User(1, "Bob", 25)], users);
    }
    
    [Fact]
    public async Task WhereLessThan()
    {
        await db.Users.Insert(new User(1, "Bob", 25));
        await db.Users.Insert(new User(2, "Sara", 45));
        
        var users = await db.Users.Where(x => x.Age < 30).ToArrayAsync();
        
        Assert.Equal([new User(1, "Bob", 25)], users);
    }
    
    [Fact]
    public async Task WhereGreaterThan()
    {
        await db.Users.Insert(new User(1, "Bob", 25));
        await db.Users.Insert(new User(2, "Sara", 45));
        
        var users = await db.Users.Where(x => x.Age > 30).ToArrayAsync();
        
        Assert.Equal([new User(2, "Sara", 45)], users);
    }
    
    [Fact]
    public async Task WhereVariable()
    {
        await db.Users.Insert(new User(1, "Bob", 25));
        await db.Users.Insert(new User(2, "Sara", 25));
        
        var name = "Bob";
        var users = await db.Users.Where(x => x.Name == name).ToArrayAsync();
        
        Assert.Equal([new User(1, "Bob", 25)], users);
    }
    
    [Fact]
    public async Task WhereCombined()
    {
        await db.Users.Insert(new User(1, "Bob", 25));
        await db.Users.Insert(new User(2, "Sara", 25));
        
        var users = await db.Users.Where(x => x.Name == "Bob").Where(x => x.Age == 25).ToArrayAsync();
        Assert.Equal([new User(1, "Bob", 25)], users);
        
        var users2 = await db.Users.Where(x => x.Name == "Bob").Where(x => x.Age == 26).ToArrayAsync();
        Assert.Equal([], users2);
    }
    
    [Fact]
    public async Task SelectWhere()
    {
        await db.Users.Insert(new User(1, "Bob", 25));
        await db.Users.Insert(new User(2, "Sara", 25));
        
        var users = await db.Users.Where(x => x.Name == "Bob").Select(x => x.Name).ToArrayAsync();
        
        Assert.Equal(["Bob"], users);
    }
    
    [Fact]
    public async Task FirstOrDefaultOneMatch()
    {
        await db.Users.Insert(new User(1, "Bob", 25));
        await db.Users.Insert(new User(2, "Sara", 25));
        var user = await db.Users.FirstOrDefault(x => x.Id > 1);
        Assert.Equal(user, new User(2, "Sara", 25));
    }

    [Fact]
    public async Task FirstOrDefaultOfTwoMatching()
    {
        await db.Users.Insert(new User(1, "Bob", 25));
        await db.Users.Insert(new User(2, "Sara", 25));
        var user = await db.Users.FirstOrDefault(x => x.Id > 0);
        Assert.Equal(user, new User(1, "Bob", 25));
    }

    [Fact]
    public async Task FirstOrDefaultOfNoMatching()
    {
        await db.Users.Insert(new User(1, "Bob", 25));
        await db.Users.Insert(new User(2, "Sara", 25));
        var user = await db.Users.FirstOrDefault(x => x.Id > 9999);
        Assert.Null(user);
    }
    
    [Fact]
    public async Task Delete()
    {
        await db.Users.Insert(new User(1, "Bob", 25));
        await db.Users.Insert(new User(2, "Sara", 25));
        
        var result = await db.Users.Delete(x => x.Id == 1);
        Assert.Equal(1, result);
        
        var users = await db.Users.ToArrayAsync();
        
        Assert.Equal(
            [new User(2, "Sara", 25)],
            users
        );
    }
    
    [Fact]
    public async Task Update()
    {
        await db.Users.Insert(new User(1, "Bob", 25));
        await db.Users.Insert(new User(2, "Sara", 25));
        
        var result = await db.Users.Set(x => x.Name, "Bubba").Set(x => x.Age, x => x.Age + 1).Where(x => x.Id == 1);
        Assert.Equal(1, result);
        
        var user = await db.Users.FirstOrDefault(x => x.Id == 1);
        
        Assert.Equal(
            new User(1, "Bubba", 26),
            user
        );
    }
    
    [Fact]
    public async Task OrderBy()
    {
        var bob = new User(1, "Bob", 30);
        var sara = new User(2, "Sara", 20);
        await db.Users.Insert(bob, sara);
        
        var users = await db.Users.OrderBy(x => x.Age).ToArrayAsync();
            
        Assert.Equal(
            [sara, bob],
            users
        );
    }
    
    [Fact]
    public async Task OrderByThenBy()
    {
        var bob = new User(2, "Bob", 30);
        var sara = new User(1, "Sara", 30);
        await db.Users.Insert(bob, sara);
        
        var users = await db.Users.OrderBy(x => x.Age).ThenBy(x => x.Id).ToArrayAsync();
            
        Assert.Equal(
            [sara, bob],
            users
        );
    }
    
    [Fact]
    public async Task OrderByDesc()
    {
        var bob = new User(1, "Bob", 30);
        var sara = new User(2, "Sara", 30);
        await db.Users.Insert(bob, sara);
        
        var users = await db.Users.OrderByDesc(x => x.Id).ToArrayAsync();
            
        Assert.Equal(
            [sara, bob],
            users
        );
    }
    
    [Fact]
    public async Task OrderByThenByDesc()
    {
        var bob = new User(1, "Bob", 30);
        var sara = new User(2, "Sara", 30);
        await db.Users.Insert(bob, sara);
        
        var users = await db.Users.OrderBy(x => x.Age).ThenByDesc(x => x.Id).ToArrayAsync();
            
        Assert.Equal(
            [sara, bob],
            users
        );
    }
    
    [Fact]
    public async Task GroupBy()
    {
        var bob = new User(1, "Bob", 30);
        var sara = new User(2, "Sara", 20);
        var alice = new User(3, "Alice", 20);
        await db.Users.Insert(bob, sara);
        
        var users = await db.Users.GroupBy(x => x.Age).Select(x => x.Age).ToArrayAsync();
        
        Assert.Equal(
            [30, 20],
            users
        );
    }
    
    [Fact]
    public async Task GroupByAggregate()
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
    
    private class TestDbContext(DbOptionsTyped options) : DbContext(options)
    {
        public Table<User> Users { get; } = new(options);
    }
    
    private record User(
        int Id,
        string Name,
        int Age
    );
}