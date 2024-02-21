#region

using System.Data.Common;
using Cooke.Gnissel.Npgsql;
using Cooke.Gnissel.Services.Implementations;
using Cooke.Gnissel.Typed;
using Cooke.Gnissel.Utils;
using Npgsql;

#endregion

namespace Cooke.Gnissel.Test;

public class LongRunningTransactionTests
{
    private readonly NpgsqlDataSource _dataSource = Fixture.DataSourceBuilder.Build();
    private TestDbContext _db;

    [OneTimeSetUp]
    public async Task Setup()
    {
        _db = new TestDbContext(NpgsqlDbOptionsFactory.Create(_dataSource));

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
        await _db.Transaction(async trans => await trans.Users.Insert(new User(0, "Bob", 25)));

        var results = await _db.Query<User>($"SELECT * FROM users WHERE name={"Bob"}")
            .ToArrayAsync();
        CollectionAssert.AreEqual(new[] { new User(0, "Bob", 25) }, results);
    }

    [Test]
    public async Task BatchInTransaction()
    {
        await _db.Transaction(async trans =>
        {
            await trans.Batch(
                trans.Users.Insert(new User(1, "Bob", 25)),
                trans.Users.Insert(new User(2, "Alice", 30)),
                trans.Users.Insert(new User(3, "Slurf", 35))
            );
        });

        var results = await _db.Query<User>($"SELECT * FROM users").ToArrayAsync();
        CollectionAssert.AreEqual(
            new[] { new User(1, "Bob", 25), new User(2, "Alice", 30), new User(3, "Slurf", 35) },
            results
        );
    }

    [Test]
    public async Task BatchInTransactionAbort()
    {
        try
        {
            await _db.Transaction(async trans =>
            {
                await trans.Batch(
                    trans.Users.Insert(new User(1, "Bob", 25)),
                    trans.Users.Insert(new User(2, "Alice", 30)),
                    trans.Users.Insert(new User(3, "Slurf", 35))
                );

                await trans.Users.Insert(new User(3, "Slurf", 35)); // Throws on colliding id
            });
        }
        catch (DbException) { }

        var results = await _db.Query<User>($"SELECT * FROM users").ToArrayAsync();
        CollectionAssert.IsEmpty(results);
    }

    [Test]
    public async Task TransactionAbort()
    {
        try
        {
            await _db.Transaction(async trans =>
            {
                await trans.Users.Insert(new User(0, "Bob", 25));
                await trans.Users.Insert(new User(0, "Bob", 25));
            });
        }
        catch (DbException) { }

        var results = await _db.Query<User>($"SELECT * FROM users WHERE name={"Bob"}")
            .ToArrayAsync();
        CollectionAssert.IsEmpty(results);
    }

    private class TestDbContext : DbContext
    {
        private readonly DbOptions _options;

        public TestDbContext(DbOptions options)
            : base(options)
        {
            _options = options;
            Users = new Table<User>(options);
        }

        private TestDbContext(TestDbContext context, DbOptions options)
            : base(options)
        {
            _options = options;
            Users = new Table<User>(context.Users, options);
        }

        public Table<User> Users { get; }

        public async Task Transaction(
            Func<TestDbContext, Task> action,
            CancellationToken cancellationToken = default
        )
        {
            var dbAdapter = _options.DbAdapter;
            await using var connection = _options.DbConnector.CreateConnection();
            await connection.OpenAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            var transactionCommandFactory = new FixedConnectionDbConnector(connection, dbAdapter);
            var transactionDbOptions = _options with { DbConnector = transactionCommandFactory };
            await action(new TestDbContext(this, transactionDbOptions));
            await transaction.CommitAsync(cancellationToken);
        }
    }

    public record User(int Id, string Name, int Age);
}
