using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;

namespace Cooke.Gnissel;

public class Column<TTable, TCol> : IColumn<TTable>
{
    private readonly IDbAdapter _dbAdapter;
    private readonly Func<TTable, TCol> _getter;

    public Column(IDbAdapter dbAdapter, string name, bool isIdentity, MemberInfo member)
    {
        _dbAdapter = dbAdapter;
        Name = name;
        IsIdentity = isIdentity;
        Member = member;

        var tableItemParameter = Expression.Parameter(typeof(TTable));
        var propertyInfo = (PropertyInfo)member;
        _getter =
            (Func<TTable, TCol>)
                Expression
                    .Lambda(
                        Expression.GetFuncType(typeof(TTable), propertyInfo.PropertyType),
                        Expression.Property(tableItemParameter, propertyInfo),
                        tableItemParameter
                    )
                    .Compile();
    }

    public DbParameter CreateParameter(TTable item) => _dbAdapter.CreateParameter(_getter(item));

    public string Name { get; }

    public bool IsIdentity { get; }

    public MemberInfo Member { get; }
}
