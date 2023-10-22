using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.Contracts;
using System.Reflection;
using Cooke.Gnissel.Utils;

namespace Cooke.Gnissel;

public record Table<T>(DbContext DbContext, DbAdapter DbAdapter, ImmutableArray<IColumn<T>> Columns)
    : QueryStatement<T>(DbContext, null, Columns)
{
    public string Name { get; } = typeof(T).Name.ToLower() + "s";

    public Table(DbContext dbContext, DbAdapter dbAdapter)
        : this(
            dbContext,
            dbAdapter,
            typeof(T)
                .GetProperties()
                .Select(p =>
                {
                    return (IColumn<T>)
                        Activator.CreateInstance(
                            typeof(Column<,>).MakeGenericType(typeof(T), p.PropertyType),
                            dbAdapter,
                            p.Name,
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
            DbAdapter,
            this,
            insertColumns,
            insertColumns.Select(col => col.CreateParameter(item))
        );
    }
}
