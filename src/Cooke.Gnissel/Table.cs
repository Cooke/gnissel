using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.Contracts;
using System.Reflection;
using Cooke.Gnissel.CommandFactories;
using Cooke.Gnissel.Services;
using Cooke.Gnissel.Services.Implementations;
using Cooke.Gnissel.Statements;
using Cooke.Gnissel.Utils;

namespace Cooke.Gnissel;

public class Table<T> : QueryStatement<T>
{
    private readonly IDbAdapter _dbAdapter;
    private readonly ICommandFactory _commandFactory;
    private readonly string _name = typeof(T).Name.ToLower() + "s";
    private readonly string _insertCommandText;

    public Table(DbOptions options)
        : base(options, typeof(T).Name.ToLower() + "s", null, CreateColumns(options.DbAdapter))
    {
        _dbAdapter = options.DbAdapter;
        _commandFactory = options.CommandFactory;

        var sql = new Sql(20 + Columns.Length * 4);
        sql.AppendLiteral("INSERT INTO ");
        sql.AppendLiteral(_dbAdapter.EscapeIdentifier(Name));
        sql.AppendLiteral(" (");
        var firstColumn = true;
        foreach (var column in Columns.Where(x => !x.IsIdentity))
        {
            if (!firstColumn)
            {
                sql.AppendLiteral(", ");
            }
            sql.AppendLiteral(_dbAdapter.EscapeIdentifier(column.Name));
            firstColumn = false;
        }
        sql.AppendLiteral(") VALUES(");
        bool firstParam = true;
        foreach (var dbParameter in Columns.Where(x => !x.IsIdentity))
        {
            if (!firstParam)
            {
                sql.AppendLiteral(", ");
            }
            sql.AppendFormatted(dbParameter);
            firstParam = false;
        }
        sql.AppendLiteral(")");
        _insertCommandText = _dbAdapter.CompileSql(sql).CommandText;
    }

    public Table(Table<T> source, DbOptions options)
        : base(options, source._name, null, source.Columns)
    {
        _dbAdapter = options.DbAdapter;
        _commandFactory = options.CommandFactory;
        _insertCommandText = source._insertCommandText;
    }

    public string Name => _name;

    private static ImmutableArray<Column<T>> CreateColumns(IDbAdapter dbAdapter)
    {
        return typeof(T)
            .GetProperties()
            .Select(
                p =>
                    new Column<T>(
                        dbAdapter,
                        dbAdapter.GetColumnName(p),
                        p.GetCustomAttribute<DatabaseGeneratedAttribute>()
                            ?.Let(
                                x => x.DatabaseGeneratedOption == DatabaseGeneratedOption.Identity
                            ) ?? false,
                        p
                    )
            )
            .ToImmutableArray();
    }

    [Pure]
    public ExecuteStatement Insert(T item)
    {
        var insertColumns = Columns.Where(x => !x.IsIdentity);

        var sql = new CompiledSql(
            _insertCommandText,
            insertColumns.Select(col => col.CreateParameter(item)).ToArray()
        );
        return new ExecuteStatement(_commandFactory, sql, CancellationToken.None);
    }
}
