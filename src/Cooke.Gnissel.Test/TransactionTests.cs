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
        await _db.Transaction(async trans =>
        {
            await trans.Users.Insert(new User(0, "Bob", 25));
        });

        var results = await _db.Query<User>($"SELECT * FROM users WHERE name={"Bob"}")
            .ToArrayAsync();
        CollectionAssert.AreEqual(new[] { new User(1, "Bob", 25) }, results);
    }

    [Test]
    public async Task TransactionAbort()
    {
        await _db.Transaction(async trans =>
        {
            await trans.Users.Insert(new User(0, "Bob", 25));
        });

        var results = await _db.Query<User>($"SELECT * FROM users WHERE name={"Bob"}")
            .ToArrayAsync();
        CollectionAssert.IsEmpty(results);
    }

    private class TestDbContext : DbContext, TransactionalDbContext<TestDbContext>
    {
        public TestDbContext(DbConnectionProvider connectionProvider, DbAdapter dataDbAdapter)
            : base(dataDbAdapter, connectionProvider) { }

        public Table<User> Users => Table<User>();

        public TestDbContext WithConnectionProvider(DbConnectionProvider connectionProvider) =>
            new TestDbContext(connectionProvider, DbAdapter);
    }

    public record User(
        [property: DatabaseGenerated(DatabaseGeneratedOption.Identity)] int Id,
        string Name,
        int Age
    );
}
