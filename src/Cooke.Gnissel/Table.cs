using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.Contracts;
using System.Reflection;
using Cooke.Gnissel.Utils;

namespace Cooke.Gnissel;

public class Table<T> : QueryStatement<T>
{
    private readonly DbAdapter _dbAdapter;
    private readonly DbConnectionProvider _dbConnectionProvider;
    private readonly string _name = typeof(T).Name.ToLower() + "s";

    public Table(
        DbContext dbContext,
        DbAdapter dbAdapter,
        DbConnectionProvider dbConnectionProvider,
        ImmutableArray<IColumn<T>> columns
    )
        : base(dbAdapter, typeof(T).Name.ToLower() + "s", dbContext, null, columns)
    {
        _dbAdapter = dbAdapter;
        _dbConnectionProvider = dbConnectionProvider;
    }

    public string Name => _name;

    public Table(
        DbContext dbContext,
        DbAdapter dbAdapter,
        DbConnectionProvider dbConnectionProvider
    )
        : this(
            dbContext,
            dbAdapter,
            dbConnectionProvider,
            typeof(T)
                .GetProperties()
                .Select(p =>
                {
                    return (IColumn<T>)
                        Activator.CreateInstance(
                            typeof(Column<,>).MakeGenericType(typeof(T), p.PropertyType),
                            dbAdapter,
                            dbAdapter.GetColumnName(p),
                            p.GetCustomAttribute<DatabaseGeneratedAttribute>()
                                ?.Let(
                                    x =>
                                        x.DatabaseGeneratedOption
                                        == DatabaseGeneratedOption.Identity
                                ) ?? false,
                            p
                        )!;
                })
                .ToImmutableArray()
        ) { }

    [Pure]
    public InsertStatement<T> Insert(T item)
    {
        var insertColumns = Columns.Where(x => !x.IsIdentity);

        return new InsertStatement<T>(
            _dbConnectionProvider,
            _dbAdapter,
            this,
            insertColumns,
            insertColumns.Select(col => col.CreateParameter(item))
        );
    }
}
