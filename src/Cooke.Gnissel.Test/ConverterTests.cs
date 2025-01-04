using System.Collections.Immutable;
using System.Data.Common;
using Cooke.Gnissel.AsyncEnumerable;
using Cooke.Gnissel.Converters;
using Cooke.Gnissel.Npgsql;
using Cooke.Gnissel.Services.Implementations;
using Cooke.Gnissel.Typed;
using Npgsql;

namespace Cooke.Gnissel.Test;

public class ConverterTests
{
    private readonly NpgsqlDataSource _dataSource = Fixture.DataSourceBuilder.Build();

    [OneTimeSetUp]
    public async Task Setup()
    {
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

    private TestDbContext CreateDb(IImmutableList<DbConverter> converters)
    {
        var adapter = new NpgsqlDbAdapter(_dataSource);

        return new TestDbContext(
            new(adapter, new ExpressionObjectReaderProvider(adapter), converters)
        );
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
        var db = CreateDb([]);
        await db.NonQuery(
            $"INSERT INTO users (name, age) VALUES ({new Name("Bob")}, {new Age(25)})"
        );
        var results = await db.Query<Name>($"SELECT name FROM users").ToArrayAsync();
        CollectionAssert.AreEqual(new[] { new Name("Bob") }, results);
    }

    [Test]
    public async Task NullableNestedValueConverter()
    {
        var db = CreateDb([]);
        await db.NonQuery(
            $"INSERT INTO users (name, age) VALUES ({new Name("Bob")}, {new Age(25)})"
        );
        var results = await db.Query<Name?>($"SELECT NULL").ToArrayAsync();
        CollectionAssert.AreEqual(new[] { (Name?)null }, results);
    }

    [Test]
    public async Task NestedValueMemberConverting()
    {
        var db = CreateDb([]);
        await db.NonQuery(
            $"INSERT INTO users (name, age) VALUES ({new Name("Bob")}, {new Age(25)})"
        );
        var results = await db.Query<User>($"SELECT name, age FROM users").ToArrayAsync();
        CollectionAssert.AreEqual(new[] { new User(new("Bob"), new Age(25)) }, results);
    }

    [Test]
    public async Task EnumGlobalConverter()
    {
        var db = CreateDb([new EnumStringDbConverter()]);
        await db.NonQuery($"INSERT INTO users (name) VALUES ({NameEnum.Bob})");
        var results = await db.Query<NameEnum>($"SELECT name FROM users").ToArrayAsync();
        CollectionAssert.AreEqual(new[] { NameEnum.Bob }, results);
    }

    [Test]
    public async Task CustomGlobalConverter()
    {
        var db = CreateDb([new NameEnumDbConverter()]);
        await db.NonQuery($"INSERT INTO users (name) VALUES ({NameEnum.Bob})");
        var results = await db.Query<NameEnum>($"SELECT name FROM users").ToArrayAsync();
        CollectionAssert.AreEqual(new[] { NameEnum.Bob }, results);
    }

    private class TestDbContext(DbOptions options) : DbContext(options)
    {
        public Table<User> Users { get; } = new(options);
    }

    private record User(Name Name, Age Age);

    [DbConverter(typeof(NestedValueDbConverter))]
    private record Age(int Value);

    [DbConverter(typeof(NestedValueDbConverter))]
    private record Name(string Value);

    private enum NameEnum
    {
        Bob,
    }

    private class NameEnumDbConverter : ConcreteDbConverter<NameEnum>
    {
        public override DbValue ToValue(NameEnum value) =>
            new DbValue<string>(value.ToString(), null);

        public override NameEnum FromReader(DbDataReader reader, int ordinal) =>
            Enum.TryParse(reader.GetString(ordinal), false, out NameEnum value)
                ? value
                : throw new DbConvertException(reader.GetFieldType(ordinal), typeof(NameEnum));
    }
}
