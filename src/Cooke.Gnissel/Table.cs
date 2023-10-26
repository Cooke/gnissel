using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.Contracts;
using System.Reflection;
using Cooke.Gnissel.Utils;

namespace Cooke.Gnissel;

public class Table<T> : QueryStatement<T>
{
    private readonly IDbAdapter _dbAdapter;
    private readonly ICommandProvider _commandProvider;
    private readonly string _name = typeof(T).Name.ToLower() + "s";

    public Table(DbOptions options)
        : base(options, typeof(T).Name.ToLower() + "s", null, CreateColumns(options.DbAdapter))
    {
        _dbAdapter = options.DbAdapter;
        _commandProvider = options.CommandProvider;
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
    public InsertStatement<T> Insert(T item)
    {
        var insertColumns = Columns.Where(x => !x.IsIdentity);

        return new InsertStatement<T>(
            _commandProvider,
            _dbAdapter,
            this,
            insertColumns,
            insertColumns.Select(col => col.CreateParameter(item))
        );
    }
}
