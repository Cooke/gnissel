#region

using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Cooke.Gnissel.AsyncEnumerable;
using Cooke.Gnissel.History;
using Cooke.Gnissel.Npgsql;
using Cooke.Gnissel.Typed;
using Cooke.Gnissel.Utils;
using Npgsql;

#endregion

namespace Cooke.Gnissel.Test;

public class MigrationTests
{
    private readonly NpgsqlDataSource _dataSource = Fixture.DataSourceBuilder.Build();
    private DbContext _db;

    [OneTimeSetUp]
    public void Setup()
    {
        _db = new DbContext(new(new NpgsqlDbAdapter(_dataSource)));
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _dataSource.CreateCommand("DROP TABLE users").ExecuteNonQuery();
    }

    [Test]
    public async Task Migrate()
    {
        var migrations = new List<Migration>()
        {
            new Migration(
                "create users table",
                async (db, ct) =>
                    await db.NonQuery(
                            $"""
                                create table users
                                (
                                    id   integer primary key generated always as identity
                                );
                            """
                        )
                        .ExecuteAsync(ct)
            )
        };
        await _db.Migrate(migrations);
        Assert.DoesNotThrowAsync(
            async () => await _db.Query<string>($"SELECT id FROM users").ToArrayAsync()
        );

        Assert.ThrowsAsync<PostgresException>(
            async () => await _db.Query<string>($"SELECT name FROM users").ToArrayAsync()
        );

        migrations.Add(
            new Migration(
                "add name to users table",
                async (db, ct) =>
                    await db.NonQuery($"ALTER TABLE users ADD COLUMN name text").ExecuteAsync(ct)
            )
        );

        await _db.Migrate(migrations);
        Assert.DoesNotThrowAsync(
            async () => await _db.Query<string>($"SELECT name FROM users").ToArrayAsync()
        );
    }
}
