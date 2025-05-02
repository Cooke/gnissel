#region

using System.Data.Common;
using Cooke.Gnissel.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

#endregion

namespace Cooke.Gnissel.Npgsql;

public sealed partial class NpgsqlDbAdapter(
    NpgsqlDataSource dataSource,
    ILogger? logger = null,
    IDbNameProvider? nameProvider = null
) : IDbAdapter
{
    private readonly ILogger? _logger = logger ?? new NullLogger<NpgsqlDbAdapter>();
    private readonly IDbNameProvider _nameProvider = nameProvider ?? new SnakeCaseDbNameProvider();

    public NpgsqlDbAdapter(NpgsqlDataSource dataSource)
        : this(dataSource, NullLogger.Instance) { }

    public DbParameter CreateParameter<TValue>(TValue value, string? dbType) =>
        typeof(TValue) == typeof(object)
            ? new NpgsqlParameter { Value = value, DataTypeName = dbType }
            : new NpgsqlParameter<TValue> { TypedValue = value, DataTypeName = dbType };

    public DbBatchCommand CreateBatchCommand() => new NpgsqlBatchCommand();

    public DbCommand CreateCommand() => new NpgsqlCommand();

    public IDbConnector CreateConnector() => new NpgsqlDbConnector(dataSource);
}
