#region

using System.Collections.Immutable;
using System.Data.Common;
using Cooke.Gnissel.Npgsql;
using Cooke.Gnissel.Services;
using Npgsql;

#endregion

namespace Cooke.Gnissel.Test;

public class SourceGeneration
{
    private readonly NpgsqlDataSource _dataSource = Fixture.DataSourceBuilder
    // .EnableDynamicJsonMappings()
    .Build();
    private DbContext _db;

    [OneTimeSetUp]
    public async Task Setup()
    {
        _db = new DbContext(new(new NpgsqlDbAdapter(_dataSource)));

        await _dataSource
            .CreateCommand(
                """
                    create table users
                    (
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
        _dataSource.CreateCommand("TRUNCATE devices RESTART IDENTITY CASCADE").ExecuteNonQuery();
    }

    [Test]
    public async Task Query()
    {
        await _db.NonQuery($"INSERT INTO users (name, age) VALUES ('Bob', 25)");
        var result = await _db.QuerySingle<User>($"SELECT * FROM users");
        Assert.AreEqual(new User("Bob", 25), result);
    }

    [DbRead]
    private record User(string Name, int Age);

    [DbRead]
    private record Device(string Name);

    private class GeneratedObjectReaderProvider(IDbAdapter adapter) : IObjectReaderProvider
    {
        public ObjectReader<TOut> Get<TOut>(DbOptions dbOptions)
        {
            var objectReader = typeof(TOut).Name switch
            {
                "User" => _userReader,
                "Device" => _deviceReader,
                _ => throw new NotSupportedException(
                    "No reader found for type " + typeof(TOut).Name
                ),
            };
            return (ObjectReader<TOut>)(object)objectReader;
        }

        private readonly ObjectReader<User> _userReader =
            new(ReadUser, [.. ReadUserPaths.Select(adapter.ToColumnName)]);

        private readonly ObjectReader<User> _deviceReader =
            new(ReadUser, [.. ReadDevicePaths.Select(adapter.ToColumnName)]);

        private static readonly ImmutableArray<PathSegment> ReadUserPaths =
        [
            new ParameterPathSegment("name"),
            new ParameterPathSegment("age"),
        ];

        private static User ReadUser(DbDataReader reader, IReadOnlyList<int> columnOrdinals) =>
            new(
                reader.GetString(
                    columnOrdinals[0] /* name */
                ),
                reader.GetInt32(
                    columnOrdinals[0] /* age */
                )
            );

        private static readonly ImmutableArray<PathSegment> ReadDevicePaths =
        [
            new ParameterPathSegment("name"),
        ];

        private static Device ReadDevice(DbDataReader reader, IReadOnlyList<int> columnOrdinals) =>
            new(
                reader.GetString(
                    columnOrdinals[0] /* name */
                )
            );
    }
}
