#region

using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Cooke.Gnissel.Npgsql;
using Cooke.Gnissel.PlusPlus;
using Cooke.Gnissel.Services.Implementations;
using Cooke.Gnissel.Utils;
using Dapper;
using Npgsql;

#endregion

namespace Cooke.Gnissel.Test;

[Explicit]
public class PerformanceTests
{
    private readonly NpgsqlDataSource _dataSource = Fixture.DataSourceBuilder
    // .EnableDynamicJsonMappings()
    .Build();
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
                    id   integer primary key generated always as identity,
                    name text,
                    age  integer,
                    str1 text,
                    str2 text,
                    str3 text,
                    str4 text,
                    str5 text,
                    str6 text,
                    str7 text,
                    str8 text,
                    int1 integer,
                    int2 integer,
                    int3 integer,
                    int4 integer,
                    int5 integer,
                    int6 integer,
                    int7 integer,
                    int8 integer
                );
                """
            )
            .ExecuteNonQueryAsync();

        var rand = new Random();
        var inserts = new List<User>();
        for (int i = 0; i < 10000; i++)
        {
            inserts.Add(
                new User(
                    0,
                    "Bob" + i,
                    25 - (i % 10),
                    Guid.NewGuid().ToString(),
                    Guid.NewGuid().ToString(),
                    Guid.NewGuid().ToString(),
                    Guid.NewGuid().ToString(),
                    Guid.NewGuid().ToString(),
                    Guid.NewGuid().ToString(),
                    Guid.NewGuid().ToString(),
                    Guid.NewGuid().ToString(),
                    rand.Next(),
                    rand.Next(),
                    rand.Next(),
                    rand.Next(),
                    rand.Next(),
                    rand.Next(),
                    rand.Next(),
                    rand.Next()
                )
            );
        }

        await _db.Batch(inserts.Chunk(1000).Select(_db.Users.Insert));

        // Warm-up
        await QueryDapper();
        await QueryGnissel();
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
    public async Task QueryGnissel()
    {
        await _db.Query<User>($"SELECT * FROM users").ToArrayAsync();
    }

    [Test]
    public async Task QueryDapper()
    {
        await using var connection = _dataSource.CreateConnection();
        var result = await connection.QueryAsync<User>($"SELECT * FROM users");
        var array = result.ToArray();
    }

    private class TestDbContext(DbOptions options) : DbContext(options)
    {
        public Table<User> Users { get; } = new(options);
    }

    private record User(
        [property: DatabaseGenerated(DatabaseGeneratedOption.Identity)] int Id,
        string Name,
        int Age,
        string Str1,
        string Str2,
        string Str3,
        string Str4,
        string Str5,
        string Str6,
        string Str7,
        string Str8,
        int Int1,
        int Int2,
        int Int3,
        int Int4,
        int Int5,
        int Int6,
        int Int7,
        int Int8
    );
}
