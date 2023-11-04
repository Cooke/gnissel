using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.Contracts;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Cooke.Gnissel.CommandFactories;
using Cooke.Gnissel.Services;
using Cooke.Gnissel.Services.Implementations;
using Cooke.Gnissel.Statements;
using Cooke.Gnissel.Utils;

namespace Cooke.Gnissel;

public class Table<T> : TableQueryStatement<T>
{
    private readonly IDbAdapter _dbAdapter;
    private readonly IDbAccessFactory _dbAccessFactory;
    private readonly string _name = typeof(T).Name.ToLower() + "s";
    private readonly string _insertCommandText;

    public Table(DbOptions options)
        : base(options, typeof(T).Name.ToLower() + "s", null, CreateColumns(options.DbAdapter))
    {
        _dbAdapter = options.DbAdapter;
        _dbAccessFactory = options.DbAccessFactory;

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
        _dbAccessFactory = options.DbAccessFactory;
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
                        dbAdapter.DefaultIdentifierMapper.ToColumnName(p),
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
        return new ExecuteStatement(_dbAccessFactory, sql, CancellationToken.None);
    }

    [Pure]
    public ExecuteStatement Insert(params T[] items) => Insert((IEnumerable<T>)items);

    [Pure]
    public ExecuteStatement Insert(IEnumerable<T> items)
    {
        var insertColumns = Columns.Where(x => !x.IsIdentity).ToArray();
        var itemsArray = items as ICollection<T> ?? (ICollection<T>)items.ToArray();
        var sb = new StringBuilder(
            _insertCommandText.Length + insertColumns.Length * 6 * itemsArray.Count
        );
        sb.Append(_insertCommandText);
        for (var i = 1; i < itemsArray.Count; i++)
        {
            sb.Append(", (");
            for (var j = 0; j < insertColumns.Length; j++)
            {
                if (j > 0)
                {
                    sb.Append(", ");
                }

                sb.Append('$');
                sb.Append(i * insertColumns.Length + j + 1);
            }
            sb.Append(") ");
        }

        var sql = new CompiledSql(
            sb.ToString(),
            itemsArray
                .SelectMany(item => insertColumns.Select(col => col.CreateParameter(item)))
                .ToArray()
        );
        return new ExecuteStatement(_dbAccessFactory, sql, CancellationToken.None);
    }
}
