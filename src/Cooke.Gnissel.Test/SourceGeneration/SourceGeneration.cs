#region

using Cooke.Gnissel.Npgsql;
using Npgsql;

#endregion

namespace Cooke.Gnissel.Test;

public partial class SourceGeneration
{
    private readonly NpgsqlDataSource _dataSource = Fixture.DataSourceBuilder
    // .EnableDynamicJsonMappings()
    .Build();
    private DbContext _db;

    [OneTimeSetUp]
    public async Task Setup()
    {
        _db = new DbContext(new(new NpgsqlDbAdapter(_dataSource)));

        await _dataSource
            .CreateCommand(
                """
                    create table users
                    (
                        name text,
                        age  integer
                    );
                """
            )
            .ExecuteNonQueryAsync();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _dataSource.CreateCommand("DROP TABLE users").ExecuteNonQuery();
    }

    [TearDown]
    public void TearDown()
    {
        _dataSource.CreateCommand("TRUNCATE users RESTART IDENTITY CASCADE").ExecuteNonQuery();
        _dataSource.CreateCommand("TRUNCATE devices RESTART IDENTITY CASCADE").ExecuteNonQuery();
    }

    [Test]
    public async Task Query()
    {
        await _db.NonQuery($"INSERT INTO users (name, age) VALUES ('Bob', 25)");
        var result = await _db.QuerySingle<User>($"SELECT * FROM users");
        Assert.AreEqual(new User("Bob", 25), result);
    }

    [DbRead]
    private record User(string Name, int Age);

    [DbRead]
    private record Device(string Name);

    //
    public partial class MyDbContext { }
}
