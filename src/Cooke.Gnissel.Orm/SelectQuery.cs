#region

using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using System.Reflection;
using Cooke.Gnissel.Services;
using Cooke.Gnissel.Utils;

#endregion

namespace Cooke.Gnissel.Statements;

public class SelectQuery<T> : IAsyncEnumerable<T>
{
    private readonly IDbAdapter _dbAdapter;
    private readonly IDbConnector _dbConnector;
    private readonly string _table;
    private readonly IReadOnlyCollection<string> _expressions;
    private readonly IObjectReaderProvider _objectReaderProvider;

    public SelectQuery(DbOptions options, string table, IReadOnlyCollection<string> expressions)
    {
        _objectReaderProvider = options.ObjectReaderProvider;
        _dbAdapter = options.DbAdapter;
        _dbConnector = options.DbConnector;
        _table = table;
        _expressions = expressions;
    }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new())
    {
        return ExecuteAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);
    }

    public IAsyncEnumerable<T> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var objectReader = _objectReaderProvider.Get<T>();
        return new QueryStatement<T>(
            _dbAdapter.RenderSql(CreateSql()),
            (reader, ct) => reader.ReadRows(objectReader, ct),
            _dbConnector
        );
    }

    public Sql CreateSql()
    {
        var sql = new Sql(100, 2);
        sql.AppendLiteral(
            $"SELECT {string.Join(", ", _expressions)} FROM {_dbAdapter.EscapeIdentifier(_table)}"
        );
        return sql;
    }
}
