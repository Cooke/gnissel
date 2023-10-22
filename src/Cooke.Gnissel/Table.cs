using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using System.Reflection;

namespace Cooke.Gnissel;

public record Table<T>(ProviderAdapter ProviderAdapter, ImmutableArray<IColumn<T>> Columns)
    : QueryStatement<T>(null, Columns)
{
    public Table(ProviderAdapter providerAdapter)
        : this(
            providerAdapter,
            typeof(T)
                .GetProperties()
                .Select(p =>
                {
                    return (IColumn<T>)
                        Activator.CreateInstance(
                            typeof(Column<,>).MakeGenericType(typeof(T), p.PropertyType),
                            providerAdapter,
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
            ProviderAdapter,
            this,
            insertColumns,
            insertColumns.Select(col => col.CreateParameter(item))
        );
    }

    // public InsertStatement<T> Insert<T1, T2>(
    //     Expression<Func<T, T1>> selector1,
    //     Expression<Func<T, T2>> selector2,
    //     ValueTuple<T1, T2> values
    // )
    // {
    //     return new InsertStatement<T>(item, this);
    // }


    public string Name { get; } = typeof(T).Name.ToLower() + "s";

    // private static class ColumnFactory<TTable, TCol>
    // {
    //     public Column<TTable, TCol> Create() => new Column<TTable, TCol>()
    // }
}
