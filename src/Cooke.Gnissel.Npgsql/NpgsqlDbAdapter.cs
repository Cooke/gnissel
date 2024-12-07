#region

using System.Data.Common;
using System.Reflection;
using Cooke.Gnissel.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

#endregion

namespace Cooke.Gnissel.Npgsql;

public sealed partial class NpgsqlDbAdapter(
    NpgsqlDataSource dataSource,
    ILogger? logger = null,
    IGnisselNpgsqlNameProvider? nameProvider = null
) : IDbAdapter
{
    private static readonly Type[] BuiltInTypes =
    [
        typeof(string),
        typeof(DateTime),
        typeof(DateTimeOffset),
        typeof(TimeSpan),
        typeof(Guid),
        typeof(byte[]),
    ];

    private readonly ILogger? _logger = logger ?? new NullLogger<NpgsqlDbAdapter>();
    private readonly IGnisselNpgsqlNameProvider _nameProvider =
        nameProvider ?? new GnisselNpgsqlSnakeCaseNameProvider();

    public NpgsqlDbAdapter(NpgsqlDataSource dataSource)
        : this(dataSource, NullLogger.Instance) { }

    public string ToColumnName(PathSegment path) => _nameProvider.ToColumnName(path);

    public string ToTableName(Type type) => _nameProvider.ToTableName(type);

    public DbParameter CreateParameter<TValue>(TValue value, string? dbType) =>
        typeof(TValue) == typeof(object)
            ? new NpgsqlParameter { Value = value, DataTypeName = dbType }
            : new NpgsqlParameter<TValue> { TypedValue = value, DataTypeName = dbType };

    public DbBatchCommand CreateBatchCommand() => new NpgsqlBatchCommand();

    public DbCommand CreateCommand() => new NpgsqlCommand();

    public IDbConnector CreateConnector() => new NpgsqlDbConnector(dataSource);

    public bool IsDbMapped(Type type) =>
        type.GetCustomAttribute<DbTypeAttribute>() != null
        || type.IsPrimitive
        || BuiltInTypes.Contains(type);
}
