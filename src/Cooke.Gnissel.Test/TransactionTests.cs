#region

using System.Data.Common;
using Cooke.Gnissel.AsyncEnumerable;
using Cooke.Gnissel.Npgsql;
using Cooke.Gnissel.Typed;
using Npgsql;

#endregion

namespace Cooke.Gnissel.Test;

public partial class TransactionTests
{
    private readonly NpgsqlDataSource _dataSource = Fixture.DataSourceBuilder.Build();
    private TestDbContext _db;

    [OneTimeSetUp]
    public async Task Setup()
    {
        var adapter = new NpgsqlDbAdapter(_dataSource);
        _db = new TestDbContext(new(adapter, new DbMappers()));

        await _dataSource
            .CreateCommand(
                """
                    create table users
                    (
                        id   integer primary key,
                        name text,
                        age  integer
                    );
                """
            )
            .ExecuteNonQueryAsync();
    }

    [TearDown]
    public void TearDown()
    {
        _dataSource.CreateCommand("TRUNCATE users RESTART IDENTITY CASCADE").ExecuteNonQuery();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _dataSource.CreateCommand("DROP TABLE users").ExecuteNonQuery();
    }

    [Test]
    public async Task Transaction()
    {
        await _db.Transaction(_db.Users.Insert(new User(0, "Bob", 25)));

        var results = await _db.Query<User>($"SELECT * FROM users WHERE name={"Bob"}")
            .ToArrayAsync();
        CollectionAssert.AreEqual(new[] { new User(0, "Bob", 25) }, results);
    }

    [Test]
    public async Task TransactionAbort()
    {
        try
        {
            await _db.Transaction(
                _db.Users.Insert(new User(0, "Bob", 25)),
                _db.Users.Insert(new User(0, "Bob", 25))
            );
        }
        catch (DbException) { }

        var results = await _db.Query<User>($"SELECT * FROM users WHERE name={"Bob"}")
            .ToArrayAsync();
        CollectionAssert.IsEmpty(results);
    }

    private class TestDbContext(DbOptions options) : DbContext(options)
    {
        public Table<User> Users { get; } = new(options);
    }

    public record User(int Id, string Name, int Age);

    [DbMappers(NamingConvention = NamingConvention.SnakeCase)]
    public partial class DbMappers;
}
