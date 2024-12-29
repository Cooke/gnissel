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
        var adapter = new NpgsqlDbAdapter(_dataSource);
        _db = new DbContext(new(adapter, new GeneratedObjectReaderProvider(adapter)));

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

    private record User(UserId Id, string Name, int Age, Address Address);

    private record Address(string Street, string City, string State, string Zip);

    private record UserId(int Value);

    private record Device(string Name);
}
