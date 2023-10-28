using System.Data.Common;
using Cooke.Gnissel.CommandFactories;
using Cooke.Gnissel.Npgsql;
using Npgsql;

namespace Cooke.Gnissel.Test;

public class LongRunningTransactionTests
{
    private readonly NpgsqlDataSource _dataSource = Fixture.DataSource;
    private TestDbContext _db;

    [OneTimeSetUp]
    public async Task Setup()
    {
        _db = new TestDbContext(new DbOptions(new NpgsqlDbAdapter(_dataSource)));

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
            await using var connection = dbAdapter.CreateConnection();
            await connection.OpenAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            var transactionCommandFactory = new ConnectionCommandFactory(connection, dbAdapter);
            var transactionDbOptions = _options with { CommandFactory = transactionCommandFactory };
            await action(new TestDbContext(this, transactionDbOptions));
            await transaction.CommitAsync(cancellationToken);
        }
    }

    public record User(int Id, string Name, int Age);
}
