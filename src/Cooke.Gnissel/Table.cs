using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.Contracts;
using System.Reflection;
using Cooke.Gnissel.Utils;

namespace Cooke.Gnissel;

public class Table<T> : QueryStatement<T>
{
    private readonly DbAdapter _dbAdapter;
    private readonly ICommandProvider _commandProvider;
    private readonly string _name = typeof(T).Name.ToLower() + "s";

    private Table(
        DbAdapter dbAdapter,
        ICommandProvider commandProvider,
        ImmutableArray<IColumn<T>> columns,
        ObjectMapper objectMapper
    )
        : base(
            dbAdapter,
            typeof(T).Name.ToLower() + "s",
            null,
            columns,
            objectMapper,
            commandProvider
        )
    {
        _dbAdapter = dbAdapter;
        _commandProvider = commandProvider;
    }

    public string Name => _name;

    internal Table(
        DbAdapter dbAdapter,
        ICommandProvider commandProvider,
        ObjectMapper objectMapper
    )
        : this(
            dbAdapter,
            commandProvider,
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
                .ToImmutableArray(),
            objectMapper
        ) { }

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
