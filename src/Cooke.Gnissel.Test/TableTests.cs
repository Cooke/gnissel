using System.ComponentModel.DataAnnotations.Schema;
using Cooke.Gnissel.Npgsql;
using Npgsql;

namespace Cooke.Gnissel.Test;

public class TableTests
{
    private readonly NpgsqlDataSource _dataSource = Fixture.DataSource;
    private TestDbContext _db;

    [OneTimeSetUp]
    public async Task Setup()
    {
        _db = new TestDbContext(_dataSource);

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
    public void OneTimeTearDown()
    {
        _dataSource.CreateCommand("DROP TABLE users").ExecuteNonQuery();
    }

    [TearDown]
    public void TearDown()
    {
        _dataSource.CreateCommand("TRUNCATE users RESTART IDENTITY CASCADE").ExecuteNonQuery();
    }

    [Test]
    public async Task QueryParameters()
    {
        await _db.Users.Insert(new User(0, "Bob", 25));
        var users = await _db.Users.ToArrayAsync();
        CollectionAssert.AreEqual(new[] { new User(1, "Bob", 25) }, users);
    }

    private class TestDbContext : DbContext
    {
        public TestDbContext(NpgsqlDataSource dataSource)
            : base(new NpgsqlDbAdapter(dataSource)) { }

        public Table<User> Users => Table<User>();
    }

    private record User(
        [property: DatabaseGenerated(DatabaseGeneratedOption.Identity)] int Id,
        string Name,
        int Age
    );
}
