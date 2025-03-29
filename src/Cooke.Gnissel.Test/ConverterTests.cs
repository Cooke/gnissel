using Cooke.Gnissel.AsyncEnumerable;
using Cooke.Gnissel.Npgsql;
using Npgsql;

namespace Cooke.Gnissel.Test;

public partial class ConverterTests
{
    private readonly NpgsqlDataSource _dataSource = Fixture.DataSourceBuilder.Build();
    private DbContext _dbContext;

    [OneTimeSetUp]
    public async Task Setup()
    {
        _dbContext = new DbContext(new NpgsqlDbAdapter(_dataSource), new Mappers());
        await _dataSource
            .CreateCommand(
                """
                    create table users
                    (
                        name text primary key,
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
    public async Task NestedValueConverter()
    {
        await _dbContext.NonQuery(
            $"INSERT INTO users (name, age) VALUES ({new Name("Bob")}, {new Age(25)})"
        );
        var results = await _dbContext.Query<Name>($"SELECT name FROM users").ToArrayAsync();
        CollectionAssert.AreEqual(new[] { new Name("Bob") }, results);
    }

    [Test]
    public async Task NullableNestedValueConverter()
    {
        await _dbContext.NonQuery(
            $"INSERT INTO users (name, age) VALUES ({new Name("Bob")}, {new Age(25)})"
        );
        var results = await _dbContext.Query<Name?>($"SELECT NULL").ToArrayAsync();
        CollectionAssert.AreEqual(new[] { (Name?)null }, results);
    }

    [Test]
    public async Task NestedValueMemberConverting()
    {
        await _dbContext.NonQuery(
            $"INSERT INTO users (name, age) VALUES ({new Name("Bob")}, {new Age(25)})"
        );
        var results = await _dbContext.Query<User>($"SELECT name, age FROM users").ToArrayAsync();
        CollectionAssert.AreEqual(new[] { new User(new("Bob"), new Age(25)) }, results);
    }

    private record User(Name Name, Age Age);

    private record Age(int Value);

    private record Name(string Value);

    [DbMappers]
    public partial class Mappers;
}
