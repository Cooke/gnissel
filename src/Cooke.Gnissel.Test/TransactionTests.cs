using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using Cooke.Gnissel.Npgsql;
using Npgsql;

namespace Cooke.Gnissel.Test;

public class TransactionTests
{
    private readonly NpgsqlDataSource _dataSource = Fixture.DataSource;
    private TestDbContext _db;

    [OneTimeSetUp]
    public async Task Setup()
    {
        _db = new TestDbContext(new NpgsqlDbAdapter(_dataSource));

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
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using (var trans = await connection.BeginTransactionAsync())
        {
            await _db.Users.Insert(new User(0, "Bob", 25)).ExecuteAsync(connection);
            await trans.CommitAsync();
        }

        var results = await _db.Query<User>($"SELECT * FROM users WHERE name={"Bob"}")
            .ToArrayAsync();
        CollectionAssert.AreEqual(new[] { new User(1, "Bob", 25) }, results);
    }

    [Test]
    public async Task TransactionAbort()
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using (await connection.BeginTransactionAsync())
        {
            await _db.Users.Insert(new User(0, "Bob", 25)).ExecuteAsync(connection);
        }

        var results = await _db.Query<User>($"SELECT * FROM users WHERE name={"Bob"}")
            .ToArrayAsync();
        CollectionAssert.IsEmpty(results);
    }

    private class TestDbContext : DbContext
    {
        public TestDbContext(DbAdapter dataDbAdapter)
            : base(dataDbAdapter) { }

        public Table<User> Users => Table<User>();
    }

    public record User(
        [property: DatabaseGenerated(DatabaseGeneratedOption.Identity)] int Id,
        string Name,
        int Age
    );
}
