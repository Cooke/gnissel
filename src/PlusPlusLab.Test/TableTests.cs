using Cooke.Gnissel;
using Cooke.Gnissel.Npgsql;
using Cooke.Gnissel.Test;
using Cooke.Gnissel.Utils;
using Xunit.Abstractions;

namespace PlusPlusLab.Fact;

[Collection("Database collection")]
public class TableTests : IDisposable
{
    private readonly TestDbContext db;
        
    public TableTests(DatabaseFixture databaseFixture, ITestOutputHelper testOutputHelper) 
    {
        databaseFixture.SetOutputHelper(testOutputHelper);
        db = new TestDbContext(new DbOptionsPlus(new NpgsqlDbAdapter(databaseFixture.DataSourceBuilder.Build())));
        db.Execute(
                $"""
                    create table users
                    (
                        id   integer primary key,
                        name text,
                        age  integer
                    );
                
                    create table devices
                    (
                        name text,
                        user_id  integer references users(id)
                    );
                """
            ).GetAwaiter().GetResult();
    }
    
    public void Dispose()
    {
        db.Batch(db.Execute($"DROP TABLE devices"), db.Execute($"DROP TABLE users")).GetAwaiter().GetResult();
    }
    
    [Fact]
    public async Task Insert()
    {
        //// Ideas for partial insert
        // await db.Users.Insert(x => x.Set(x.Name, "Box").Set(x.Age, 20));
        // await db.Users.Insert(x => x.Name, "Box", x => x.Age, 20);
        // await db.Users.Insert((x => x.Name, "Box"), (x => x.Age, 20));
        // await db.Users.Insert(x => Db.Values(x.Name, x.Age), x => (x.Age, 20));
        // await db.Users.Insert(x => x.Name, "Box");
        // await db.Users.Update(x => x.Set(y => y.Name, "Bob"));
        // await db.Users.Update(x => x.Name, "Box", x => x.Age, 20);
        
        await db.Users.Insert(new User(1, "Bob", 25));
        await db.Users.Insert(new User(2, "Sara", 25));
        
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
    public async Task DoubleWhere()
    {
        await db.Users.Insert(new User(1, "Bob", 25));
        await db.Users.Insert(new User(2, "Sara", 25));
        
        var users = await db.Users.Where(x => x.Name == "Bob").Where(x => x.Age == 25).ToArrayAsync();
        Assert.Equal([new User(1, "Bob", 25)], users);
        
        var users2 = await db.Users.Where(x => x.Name == "Bob").Where(x => x.Age == 26).ToArrayAsync();
        Assert.Equal([], users2);
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
    
    private class TestDbContext(DbOptionsPlus options) : DbContext(options)
    {
        public Table<User> Users { get; } = new(options);
        
        public Table<Device> Devices { get; } = new(options);
    }
    
    private record User(
        int Id,
        string Name,
        int Age
    );

    private record Device(int UserId, string Name);

    
}