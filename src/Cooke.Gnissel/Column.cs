using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using Npgsql;

namespace Cooke.Gnissel;

public class Column<TTable, TCol> : IColumn<TTable>
{
    public Column(ProviderAdapter ProviderAdapter, string Name, bool IsIdentity, MemberInfo Member)
    {
        this.ProviderAdapter = ProviderAdapter;
        this.Name = Name;

        var tableItemParameter = Expression.Parameter(typeof(TTable));

        var propertyInfo = (PropertyInfo)Member;
        Getter =
            (Func<TTable, TCol>)
                Expression
                    .Lambda(
                        Expression.GetFuncType(typeof(TTable), propertyInfo.PropertyType),
                        Expression.Property(tableItemParameter, propertyInfo),
                        tableItemParameter
                    )
                    .Compile();
        this.IsIdentity = IsIdentity;
        this.Member = Member;
    }

    public DbParameter CreateParameter(TTable item) =>
        ProviderAdapter.CreateParameter(Getter(item));

    public ProviderAdapter ProviderAdapter { get; init; }
    public string Name { get; init; }
    public Func<TTable, TCol> Getter { get; init; }
    public bool IsIdentity { get; init; }
    public MemberInfo Member { get; init; }
}
