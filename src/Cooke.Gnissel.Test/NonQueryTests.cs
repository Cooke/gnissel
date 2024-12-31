#region

using Cooke.Gnissel.Npgsql;
using Cooke.Gnissel.Services.Implementations;
using Dapper;
using Npgsql;

#endregion

namespace Cooke.Gnissel.Test;

public class NonQueryTests
{
    private readonly NpgsqlDataSource _dataSource = Fixture.DataSourceBuilder.Build();
    private DbContext _db;

    [OneTimeSetUp]
    public async Task Setup()
    {
        var adapter = new NpgsqlDbAdapter(_dataSource);
        _db = new DbContext(new(adapter, new ExpressionObjectReaderProvider(adapter)));

        await _dataSource
            .CreateCommand(
                """
                    create table users
                    (
                        id   integer primary key generated always as identity,
                        name text,
                        age  integer
                    );
                """
            )
            .ExecuteNonQueryAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _dataSource.CreateCommand("drop table users;").ExecuteNonQueryAsync();
    }

    [TearDown]
    public void TearDown()
    {
        _dataSource.CreateCommand("TRUNCATE users RESTART IDENTITY CASCADE").ExecuteNonQuery();
    }

    [Test]
    public async Task InsertOne()
    {
        var name = "Bob";
        var result = await _db.NonQuery($"INSERT INTO users(name, age) VALUES({name}, 25)");
        Assert.That(result, Is.EqualTo(1));

        var fetched = _dataSource.OpenConnection().QuerySingle<User>("SELECT * FROM users");
        Assert.That(fetched, Is.EqualTo(new User(1, "Bob", 25)));
    }

    public record User(int Id, string Name, int Age);
}
